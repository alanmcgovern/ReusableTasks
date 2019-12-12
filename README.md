# ReusableTask

[![NuGet version](https://badge.fury.io/nu/reusabletasks.svg)](https://badge.fury.io/nu/reusabletasks)
[![Build status](https://dev.azure.com/alanmcgovern0144/ReusableTasks/_apis/build/status/ReusableTasks-CI)](https://dev.azure.com/alanmcgovern0144/ReusableTasks/_build/latest?definitionId=3)

A .NET Standard 2.0 compatible library which can be used to implement zero allocation async/await. This is conceptually similar to `ValueTask<T>`, except it's compatible with .NET 2.0 and has zero ongoing allocations once the internal cache initializes.

Sample usage:
```
public async ReusableTask InitializeAsync ()
{
    if (!Initialized) {
        await LongInitializationAsync ();
        Initialized = true;
    }
}

async ReusableTask LongInitializationAsync ()
{
    // Contact a database, load from a file, etc
}

public async ReusableTask<MyData> CreateMyDataAsync ()
{
    if (!Initialized) {
        mydata = await LongMyDataCreationAsync ();
        Initialized = true;
    }
    return mydata;
}

async ReusableTask<MyData> LongMyDataCreationAsync ()
{
    // Contact a database, load from a file, etc..
    return new MyData (....);
}
```
The compiler generated async state machine for these methods is allocation-free, and results in no garbage collectable objects being created no matter how many times the methods are invoked.

The four things you cannot do with `ValueTask` you also cannot do with `ReusableTask`. The documentation can be read here in the remarks section, https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=netcore-3.0#remarks.

Unlike the documentation states for `ValueTask`, I would recommend that the default return value for any async method should be `ReusableTask` or `ReusableTask<T>`, unless benchmarking shows otherwise.
