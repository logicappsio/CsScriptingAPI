using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Web.Http;
using Newtonsoft.Json;
using System.CodeDom;
using Newtonsoft.Json.Linq;
using TRex.Metadata;

namespace CSScript.Controllers
{
    public class CompileController : ApiController
    {
        JToken args;
        [HttpPost]
        [Metadata(friendlyName:"Return String")]
        public Output String([FromBody]
        Body body)
        {
            if (body.JSON != null)
                GenerateArgs(body.JSON);

            return new Output { ContentType = "text/plain", Content = (string)RunScript(body.script, "string")};
        }


        [HttpPost]
        [Metadata(friendlyName: "Return JToken")]
        public Output JToken([FromBody] Body body)
        {          
            if (body.JSON != null)
                GenerateArgs(body.JSON);

            return new Output { ContentType = "application/json", Content = (JToken)RunScript(body.script, "JToken") };
        }

        private void GenerateArgs(IList<JToken> json)
        {
            //If the json object is empty, return
            if (json.Count == 0)
                return;
            //If there is only one JSON object, make args a JObject
            if (json.Count == 1)
                args = json.FirstOrDefault();
            //If there are multiple JSON objects, make args a JArray
            else
            {
                args = new JArray();
                foreach (var obj in json)
                {
                    ((JArray)args).Add(obj);
                }
            }
        }
        private object RunScript(string input, string type)
        {
            var sourceCode = "using System; using Newtonsoft.Json; using Newtonsoft.Json.Linq; using System.Linq; using System.Collections.Generic; namespace Script {  public class ScriptClass { public " + type + " RunScript(JToken args) { " + input + " } } }";

            //  Get a reference to the CSharp code provider
            using (var codeDomProvider = CodeDomProvider.CreateProvider("csharp"))
            {
                //  Set compile options (we want to compile in memory)
                var compileParameters = new CompilerParameters() { GenerateInMemory = true };
                compileParameters.GenerateExecutable = false;
                
                //Include library references, only when library contains System or Newtonsoft
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!asm.IsDynamic && (asm.FullName.Contains("Newtonsoft") || asm.FullName.Contains("System")))
                        compileParameters.ReferencedAssemblies.Add(asm.Location);
                }
                //  Now compile the supplied source code and compile it.
                var compileResult = codeDomProvider.CompileAssemblyFromSource(compileParameters, sourceCode);

                //  If everything goes well (in this example, I know it is correct! :-)) we get a reference to the compiled assembly.
                var compiledAssembly = compileResult.CompiledAssembly;

                //  Now, using reflection we can create an instance of our class
                var inst = compiledAssembly.CreateInstance("Script.ScriptClass");
                object[] argsv = { args };
                //  ... and call a method on it!
                var callResult = inst.GetType().InvokeMember("RunScript", BindingFlags.InvokeMethod, null, inst, argsv);
                
                return callResult;
            }
        }

        public class Body
        {
            [Metadata(friendlyName:"C# Script", Visibility = VisibilityType.Default)]
            public string script { get; set; }
            [Metadata(friendlyName: "JSON Object(s)", description: "Include a JToken to be passed into script argument.  Can be accessed in scripted as 'args'")]
            public IList<JToken> JSON {get; set;}
         }


        public class Output
        {
            public string ContentType { get; set; }
            public object Content { get; set; }
      //      public Body inputBody { get; set; }
        }

    }

    
}
