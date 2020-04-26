# Pika Core

__Pika Core__ is a simple file management system written in C# with ASP.NET Core. Its main functionality is to give anonymous access to some parts of filesystem however it is supposed to give full access to server's filesystem(but only for download purposes) to all registered users.

Developer Instance: https://dev-core.lukas-bownik.net/
Production Instance: https://core.lukas-bownik.net/

*Done features*
* Registered and anonymous access to public part of the server's filesystem,
* Registration via local built-in auth or via Google, Discord, Microsoft, GitHub,
* Authorization using Roles: Admin, FileManagerUser and User,
* Permanent download links,
* Downloading folder as zip files,
* Easy to use file view with on-write searching and interactive navigation path on the top, sorting by type: folders and files.
* Partially addedd icons in file view,
* Uploading files to public part of the filesystem,
* Other registration forms(there is no possibility to log in via Google at the moment),

*Planned features*
* Online video watching(another system will be responsible for that),
* Other archive formats(possibly 7-zip, tar.gz),
* Bookmarks - so you will be able to easily find the same resource in Pika Cloud any time.

### Technical information ###
*Technology stack*
* .NET Core 3.1,
* Template Engine "Razor",
* Identity Framework,
* HTML5 with Materialize and JS,
* Entity Framework Core.

Frameworks&libs:
* Newtonsoft,
* Microsoft Npgsql.

*Deployment*
Server OS: Debian 10

WWW: Apache 2

Core is deployed on localhost.


__Docs__ 

There will be simple public auth and filesystem access API.
