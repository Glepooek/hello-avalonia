using Xunit;

// One WinAppDriver session controls the one desktop, so the Appium tests never run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
