# Pika Core

__Pika Core__ is a simple file management system written in C# with ASP.NET Core. Its main functionality is to give anonymous access to some parts of filesystem however it is supposed to give full access to server's filesystem to all registered users.

Developer Instance: https://dev-core.lukas-bownik.net/

Production Instance: https://core.lukas-bownik.net/

*Done features*
* Registered and anonymous access to public part of the server's filesystem,
* Registration via local built-in auth or via Google, Discord, Microsoft, GitHub,
* Authorization using Roles: Admin, FileManagerUser and User,
* Permanent download links,
* Downloading folder as zip files, (actually not accessible now due to major refactor)
* Easy to use file view with on-write searching and interactive navigation path on the top, sorting by type: folders and files.
* Partially addedd icons in file view,
* Uploading files (up to 256MB) to public part of the filesystem,
* Other registration forms,

*Planned changes*
* Downloading whole folders as Archives formats(zip, possibly 7-zip, tar.gz),
* Redesign it to be a core of whole PikaCloud system (Federation pattern),
* Removing log in page(to be moved as SSO)
* Redirect to view all resources on-demand just after PikaPlayer goes live,
* Internationalization of text,
* Add dark theme,
* Redesign Browser to support mosaic view as well as detailed list.

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
