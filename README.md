# ReusableTask

[![NuGet version](https://badge.fury.io/nu/reusabletasks.svg)](https://badge.fury.io/nu/reusabletasks)

A .NET Standard 2.0 compatible library which can be used to implement zero allocation async/await. This is conceptually similar to `ValueTask<T>`, except it's compatible with .NET 2.0.

The four things you cannot do with `ValueTask` you also cannot do with `ReusableTask`. The documentation can be read here in the remarks section, https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.0#remarks.

Unlike the documentation for ValueTask, I would recommend that the default return value for any async method should be `ReusableTask` or `ReusableTask<T>`, unless benchmarking shows otherwise.
