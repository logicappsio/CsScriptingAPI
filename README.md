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
The API app has two actions - one which returns a string as plain text, the other which returns a JToken as application/json.

Both actions ask for two input parameters:

| Input | Description |
| ----- | ----- |
| Script | C# script syntax |
| JSON Object(s) | Any JSON Objects you want to be able to reference in the script via args |

## Example ##
| Step   | Info |
|----|----|
| Action | Return JToken |
| C# Scripts | return args[1]; |
| JSON Objects | [{"ID":1, "Name": "foo"}, {"ID":2, "Name": "bar"}] |
| Output | {"ID": 2, "Name": "bar"} |
