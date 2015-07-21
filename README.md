# C# Scripting API
[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploying ##
Click the "Deploy to Azure" button above.  You can create new resources or reference existing ones (resource group, gateway, service plan, etc.)  **Site Name and Gateway must be unique URL hostnames.**  The deployment script will deploy the following:
 * Resource Group (optional)
 * Service Plan (if you don't reference exisiting one)
 * Gateway (if you don't reference existing one)
 * API App (CSharpAPI)
 * API App Host (this is the site behind the api app that this github code deploys to)

## API Documentation ##
The API app has one action - Execute Script - which returns a JToken based on what the script returns.

The action has three input parameters:

| Input | Description |
| ----- | ----- |
| Script | C# script syntax |
| Context Object *(optional)* | Objects to reference in the script.  Can pass in multiple objects, but base must be a single JObject { .. }. Can be accessed in script by object key (as a JToken). |
| Libraries *(optional)* | Array of libraries to pass in and compile with script. Works from Blob/FTP Connector output of .dll files. See structure [below](#libraries-array-structure).  Default libraries can be found [here](#compiler-information) |

#### Context Object Structure ####
```javascript
{ "object1": { ... }, "object2": "value" }
```
In script could then reference object1 and object2 - both passed in as a JToken.

#### Libraries Array Structure ####
```javascript
[{"filename": "name.dll", "assembly": {Base64StringFromConnector}, "usingstatment": "using Library.Reference;"}, { ... } ] 
```

####AppDomain ####

The script executes inside of an AppDomain that includes some standard System assemblies as well as Newtonsoft.Json.  The full list can be found [here](#compiler-information)

###Trigger###
You can use the C# Script API as a trigger.  It takes a single input of "script" and will trigger the logic app (and pass result) whenever the script returns anything but `false`.  You set the frequency in which the script runs.

## Example ##
| Step   | Info |
|----|----|
| Action | Execute Script |
| C# Script | `return message;` |
| Context Object | `{"message": {"Hello": "World"}}` |
| Output | `{"Hello": "World"}` |

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

## Compiler Information ##

The following assemblies are included by default in the script:

```csharp
using System; 
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq; 
using System.Linq; 
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Xml;
```

