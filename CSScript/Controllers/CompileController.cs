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

        JToken args;
        
        [HttpPost]
        [Metadata(friendlyName: "Execute Script")]
        public Output Execute([FromBody] Body body)
        {          
            if (body.context != null)
                GenerateArgs(body.context);

            if (body.attachments != null)
                readAttachments(body.attachments);
            
            return new Output { Result = (JToken)RunScript(body.script, "JToken", body.attachments) };
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

        private void GenerateArgs(IList<JToken> json)
        {

            if (json.Count == 1)
                args = json[0];
            //     args = json;
            if(json.Count > 1)
                args = new JArray(json);

        }
        private JToken RunScript(string input, string type, Collection<Attachment> attachments)
        {
            compiled = false;
            var sourceCode = scriptIncludes +  "namespace Script {  public class ScriptClass { public " + type + " RunScript(JToken args) { " + input + " } } }";

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
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        compileParameters.ReferencedAssemblies.Add(HttpRuntime.AppDomainAppPath + @"\" + attachment.filename);
                        sourceCode = attachment.appendusing + sourceCode;
                    }
                }

                //  Now compile the supplied source code and compile it.
                var compileResult = codeDomProvider.CompileAssemblyFromSource(compileParameters, sourceCode);

                if (attachments != null)
                {
                    foreach (var attachment in attachments)
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
                object[] argsv = { args };
                //  ... and call a method on it!
                var callResult = inst.GetType().InvokeMember("RunScript", BindingFlags.InvokeMethod, null, inst, argsv);
                compiled = true;

                
                return (JToken)callResult;
            }
        }

        private void readAttachments(Collection<Attachment> attachments)
        {
            var file = System.Convert.FromBase64String(attachments.First().attachment);
            File.WriteAllBytes(HttpRuntime.AppDomainAppPath + @"\" + attachments.First().filename, file);
        }


        public class Body
        {
            [Metadata(friendlyName:"C# Script", Visibility = VisibilityType.Default)]
            public string script { get; set; }

            [Metadata(friendlyName: "Context Object(s)", description: "JSON Object(s) to be passed into script argument.  Must be an array [ ... ].  Can be referenced in scripted as 'args'")]
            public IList<JToken> context {get; set;}

            public Collection<Attachment> attachments { get; set; }

            
        }


        public class Output
        {
            public JToken Result { get; set; }
      
        }

        public class Attachment
        {
            public string filename;
            public string content;
            public string appendusing;
        }

    }

    
}
