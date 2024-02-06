# ![""](https://lukas-bownik.net/img/logos/pikacloud_core.svg)

__Pika Core__ is a simple cloud system of my own doing. It started as a basic directory contents listing. It is written in C# with ASP.NET Core.

Cloudfront: https://cloud.lukas-bownik.net/

Pika Core Landing: https://core.lukas-bownik.net/

### Versions ###
| Major Version  | Overall Description  |
|---|---|
| 1.0   |  It was just a listing of a hardcoded directory from server's filesystem. At the end of its lifetime 1st major version was actually not much more than simple sign in, registration and file browsing  | 
| 2.0  | Better visuals - till that version I used MaterializeJS instead of raw Bootstrap. Still, it was just a listing of my directories read from configuration. Authorization was implemented using local Identity store + 3rd parties like Google, Microsoft, Discord.  | 
|  3.0 | Better colour pallete, standarized colour scheme across systems in the cloud. I managed to refactor the system, thus the system uses MinIO as a storage of resources. Following that change, PikaCore "describes" everything as a resource and uses my own solution - VCOS (Virtual Categorized Object System) to manage data stored in object storage. VCOS is not standarized till the moment of commithing this change to README.  | 

### Technical information ###

*Technology stack*

* .NET 7,
* Template Engine "Razor",
* Identity Framework with Orchard Core as IdP,
* HTML5 with Materialize and JS, jQuery,
* Entity Framework Core,
* MartenDB for Event Sourcing,
* Net.EventBus,
* AutoMapper,
* Serilog with Grafana adapter.

3rd party software:
* MinIO,
* Newtonsoft,
* Microsoft Npgsql,
* FFmpeg.NET amd ffmpeg,
* Redis cache,
* PostgreSQL RDBMS,
* Docker
* Grafana and Loki for logs.

__Docs__

There will be OIDC authorization VCOS filesystem access API.
