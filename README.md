# C# Scripting API
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploying ##
Click the "Deploy to Azure" button above.  You can create new resources or reference existing ones (resource group, gateway, service plan, etc.)  **Site Name and Gateway must be unique URL hostnames.**  The deployment script will deploy the following:
 * Resource Group (optional)
 * Service Plan (if you don't reference exisiting one)
 * Gateway (if you don't reference existing one)
 * API App (CSharp.API)
 * API App Host (this is the site behind the api app that this github code deploys to)
 * Logic App Sample

## API Documentation ##
The API app has one action - Execute Script - which returns a single "Result" parameter.

The action has two input parameters:

| Input | Description |
| ----- | ----- |
| Script | C# script syntax |
| Context Object | JSON to reference in the script via `args`.  Can pass in multiple objects, but base must be a single JObject { .. } |

The script executes inside of an AppDomain that includes some standard System assemblies as well as Newtonsoft.Json.

###Trigger###
You can use the C# Script API as a trigger.  It takes a single input of "script" and will trigger the logic app (and pass result) whenever the script returns anything but `false`.  You set the frequency in which the script runs.

## Example ##
| Step   | Info |
|----|----|
| Action | Execute Script |
| C# Script | `return args["Objects"][1];` |
| Context Object | `{"Objects": [{"ID":1, "Name": "foo"}, {"ID":2, "Name": "bar"}]}` |
| Output | `{"Result": {"ID": 2, "Name": "bar"}}` |

You can also perform more complex scripts like the following:
```
double cost = args["Cost"];
for(int x = 1; x < args.length; x++) 
  {
  args["array"][x]["foo"] = (int)args["array"][x]["bar"] * cost;
  }
return args;
```
