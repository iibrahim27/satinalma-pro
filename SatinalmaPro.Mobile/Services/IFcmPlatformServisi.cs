namespace SatinalmaPro.Mobile.Services;

public interface IFcmPlatformServisi
{
    Task BaslatAsync();
    Task<string?> TokenAlAsync();
}

public sealed class FcmPlatformServisiStub : IFcmPlatformServisi
{
    public Task BaslatAsync() => Task.CompletedTask;
    public Task<string?> TokenAlAsync() => Task.FromResult<string?>(null);
}
