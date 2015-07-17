using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using TRex.Metadata;
using System.Net.Http;
using Microsoft.Azure.AppService.ApiApps.Service;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Web;
using System.IO;

namespace CSScript.Controllers
{
    public class CompileController : ApiController
    {
        private bool compiled;
        //The imports that will be included when executing the script.  Must be System or Newtonsoft (or you would need to change where the assemblies are referenced to include more)
        private const string scriptIncludes = @"
            using System; 
            using Newtonsoft.Json; 
            using Newtonsoft.Json.Linq; 
            using System.Linq; 
            using System.Collections.Generic;
            using System.Net;
            using System.Net.Http;";

        List<JToken> args;
        
        private string contextTokens = "";

        [HttpPost]
        [Metadata(friendlyName: "Execute Script")]
        public Output Execute([FromBody] Body body)
        {          
            if (body.context != null)
                GenerateArgs(body.context);

            if (body.libraries != null)
                readAttachments(body.libraries);
            var result = (JToken)RunScript(body.script, "JToken", body.libraries);
            if (compiled)
                return new Output { Result = result };
            else
            {
                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(result.ToString()),
                    ReasonPhrase = "Compiler Error"
                };
                throw new HttpResponseException(resp);
            }
           
        }

       
        [HttpGet]
        [Metadata(friendlyName: "Execute Script Trigger", description: "When the script returns true, the Logic App will fire")]
        [Trigger(TriggerType.Poll)]
        public HttpResponseMessage ExecutePollTrigger([FromUri] string script)
        {
            
            JToken result = RunScript(script, "JToken", null);
            Debug.WriteLine(result.ToString());
            if (result.ToString() == "False")
                return Request.EventWaitPoll();
            else if (compiled)
                return Request.EventTriggered(new Output { Result = result});
            else
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, result.ToString()); 
        }

        /// <summary>
        /// Generates variables within the script matching to the Key/Values of the context object
        /// </summary>
        /// <param name="json"></param>
        private void GenerateArgs(JObject json)
        {
            args = new List<JToken>();
            foreach(var param in json)
            {
                contextTokens += "JToken " + param.Key +", ";
                args.Add(param.Value);
            }
            contextTokens = contextTokens.Remove(contextTokens.Length - 2, 2);
        }
        private JToken RunScript(string input, string type, Collection<Library> libraries)
        {
            compiled = false;
            var sourceCode = scriptIncludes +  "namespace Script {  public class ScriptClass { public " + type + " RunScript(" + contextTokens + ") { " + input + " } } }";

            //  Get a reference to the CSharp code provider
            using (var codeDomProvider = CodeDomProvider.CreateProvider("csharp"))
            {
                //  Set compile options (we want to compile in memory)
                var compileParameters = new CompilerParameters() { GenerateInMemory = true };
                compileParameters.GenerateExecutable = false;
                
                //Include library references, only when library contains System or Newtonsoft
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!asm.IsDynamic && ( asm.FullName.Contains("System") || asm.FullName.Contains("Newtonsoft")))
                        compileParameters.ReferencedAssemblies.Add(asm.Location);
                }
                if (libraries != null)
                {
                    foreach (var attachment in libraries)
                    {
                        compileParameters.ReferencedAssemblies.Add(HttpRuntime.AppDomainAppPath + @"\" + attachment.filename);
                        sourceCode = attachment.usingstatement + sourceCode;
                    }
                }

                //  Now compile the supplied source code and compile it.
                var compileResult = codeDomProvider.CompileAssemblyFromSource(compileParameters, sourceCode);

                if (libraries != null)
                {
                    foreach (var attachment in libraries)
                    {
                        File.Delete(HttpRuntime.AppDomainAppPath + @"\" + attachment.filename);
                    }
                }

                //  Check for compile errors
                if (compileResult.Errors.Count > 0)
                {
                    var errorArray = new JArray();
                    foreach (var error in compileResult.Errors)
                    {
                       errorArray.Add(error.ToString());
                    }
                    return errorArray;
                }
                //  If everything goes well we get a reference to the compiled assembly.
                var compiledAssembly = compileResult.CompiledAssembly;

                //  Now, using reflection we can create an instance of our class
                var inst = compiledAssembly.CreateInstance("Script.ScriptClass");
                //  ... and call a method on it!
                var callResult = inst.GetType().InvokeMember("RunScript", BindingFlags.InvokeMethod, null, inst, args.ToArray());
                compiled = true;

                
                return (JToken)callResult;
            }
        }

        private void readAttachments(Collection<Library> libraries)
        {
            var file = System.Convert.FromBase64String(libraries.First().assembly);
            File.WriteAllBytes(HttpRuntime.AppDomainAppPath + @"\" + libraries.First().filename, file);
        }


        public class Body
        {
            [Metadata(friendlyName:"C# Script", Visibility = VisibilityType.Default)]
            public string script { get; set; }

            [Metadata(friendlyName: "Context Object(s)", description: "JSON Object(s) to be passed into script argument.  Can pass in multiple items, but base object must be single object { .. }.  Can be referenced in scripted as object names")]
            public JObject context {get; set;}

            [Metadata(friendlyName: "Libraries", description: "Libraries to be included in execution of script. Array of this format [{\"filename\": , \"assembly\": , \"usingstatement\": }, ..]", Visibility = VisibilityType.Advanced)]
            public Collection<Library> libraries { get; set; }

            
        }


        public class Output
        {
            public JToken Result { get; set; }
      
        }

        
        public class Library
        {
            public string filename;
            public string assembly;
            public string usingstatement;
        }

    }

    
}
