# C# Scripting API
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploying ##
Click the "Deploy to Azure" button above.  You can create new resources or reference existing ones (resource group, gateway, service plan, etc.)  **Site Name and Gateway must be unique URL hostnames.**  The deployment script will deploy the following:
 * Resource Group (optional)
 * Service Plan (if you don't reference exisiting one)
 * Gateway (if you don't reference existing one)
 * API App (CSharpAPI)
 * API App Host (this is the site behind the api app that this github code deploys to)
 * Logic App Sample - *This may not deploy before API builds causing some not found errors in designer*

## API Documentation ##
The API app has one action - Execute Script - which returns a single "Result" parameter.

The action has three input parameters:

| Input | Description |
| ----- | ----- |
| Script | C# script syntax |
| Context Object *(optional)* | Objects to reference in the script.  Can pass in multiple objects, but base must be a single JObject { .. }. Can be accessed in script by object key (as a JToken). |
| Libraries *(optional)* | Array of libraries to pass in and compile with script. Works from Blob/FTP Connector output of .dll files. See structure below. |

#### Context Object Structure ####
```javascript
{ "object": { ... }, "object2": { ... } }
```
In script could then reference JToken object and JToken object

#### Libraries Array Structure ####
```javascript
[{"filename": "name.dll", "assembly": {Base64StringFromConnector}, "usingstatment": "using Library.Reference;"}, { ... } ] 
```

####AppDomain ####

The script executes inside of an AppDomain that includes some standard System assemblies as well as Newtonsoft.Json.

###Trigger###
You can use the C# Script API as a trigger.  It takes a single input of "script" and will trigger the logic app (and pass result) whenever the script returns anything but `false`.  You set the frequency in which the script runs.

## Example ##
| Step   | Info |
|----|----|
| Action | Execute Script |
| C# Script | `return users;` |
| Context Object | `{"users": [{"ID":1, "Name": "foo"}, {"ID":2, "Name": "bar"}]}` |
| Output | `{"Result": [{"ID":1, "Name": "foo"}, {"ID":2, "Name": "bar"}]}` |

You can also perform more complex scripts like the following:
####Context Object####
```javascript
{ "tax": 0.06, "orders": [{"order": "order1", "subtotal": 100}] }
```
#### C\# Script ####
```csharp
foreach (var order in orders)
{
    order["total"] = (double)order["subtotal"] * (1 + (double)tax);
}
return orders;
```

#### Result ####
```javascript
[ {"order": "order1", "subtotal": 100, "total": 106.0 } ] 
```
