using System;

namespace PikaCore.Areas.Identity.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeUserBucketAccess : Attribute
{ }