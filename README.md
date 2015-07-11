# C# Scripting API
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploying ##
Click the "Deploy to Azure" button above.  You can create new resources or reference existing ones (resource group, gateway, service plan, etc.)  **Site Name and Gateway must be unique URL hostnames.**  The deployment script will deploy the following:
 * Resource Group (optional)
 * Gateway (if a new resource group and you don't reference existing one)
 * API App (CSScriptingAPI)
 * API App Host (this is the site behind the api app that this github code deploys to)
 * Logic App Sample

## API Documentation ##
The API app has one action - Execute Script - which returns a single "Result" parameter.

The action has two input parameters:

| Input | Description |
| ----- | ----- |
| Script | C# script syntax |
| JSON Object(s) | An array of JSON objects to reference in the script via `args` |

The script executes inside of an AppDomain that includes some standard System assemblies as well as Newtonsoft.Json.

###Trigger###
You can use the C# Script API as a trigger.  It takes a single input of "script" and will trigger the logic app whenever the script returns `true`.  You set the frequency in which the script runs.

## Example ##
| Step   | Info |
|----|----|
| Action | Execute Script |
| C# Script | `return args[1];` |
| JSON Objects | `[{"ID":1, "Name": "foo"}, {"ID":2, "Name": "bar"}]` |
| Output | `{"Result": {"ID": 2, "Name": "bar"}}` |

You can also perform more complex scripts like the following:
```
double cost = args[0]["Cost"]
for(int x = 1; x < args.length; x++) 
  {
  args[x]["foo"] = (int)args[x]["bar"] * cost;
  }
return args;
```
