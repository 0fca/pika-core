# FMS - File Management System aka Veronica

__Veronica__ is a simple file management system written in C# with ASP.NET Core. Its main functionality is to give anonymous access to some parts of filesystem however it is supposed to give full access to server's filesystem(but only for download purposes) to all registered users.

It is also my portfolio page.

Production: https://me.lukas-bownik.net/

*Done features*
* Registered and anonymous access to public part of the server's filesystem,
* Registration via local built-in auth or via Google,
* Authorization using Roles: Admin, FileManagerUser and User,
* Permanent download links,
* Downloading folder as zip files(in progress),
* Easy to use file view with on-write searching and interactive navigation path on the top.

*Planned features*
* Other registration forms,
* Online video watching,
* Other archive formats(possibly 7-zip, tar.gz),
* Sorting and icons for file view,
* Uploading files to public part of the filesystem.

### Technical information ###
*Technology stack*
* Core 2.1,
* Template Engine,
* Identity Framework,
* HTML5 with Bootstrap and JS,
* Entity Framework.

Frameworks&libs:
* Newtonsoft,
* MySQL Pomelo Connector.

*Deployment*
Server OS: Debian 9

WWW: Apache 2.4 as a Proxy

Core is deployed on localhost.


__Docs__ 

There will be simple public auth and filesystem access(this will be built in Spring Web) API.
