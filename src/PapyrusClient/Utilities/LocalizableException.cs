using Microsoft.Extensions.Localization;

namespace PapyrusClient.Utilities;

public class LocalizableException : Exception
{
    private readonly IStringLocalizer _localizer;
    private readonly string _resourceKey;
    private readonly string[] _resourceArgs;

    public LocalizableException(IStringLocalizer localizer, string resourceKey)
    {
        _localizer = localizer;

        _resourceKey = resourceKey;
        _resourceArgs = [];
    }

    public LocalizableException(IStringLocalizer localizer, string resourceKey, params string[] resourceArgs)
    {
        _localizer = localizer;

        _resourceKey = resourceKey;
        _resourceArgs = resourceArgs;
    }

    public LocalizableException(IStringLocalizer localizer, string resourceKey, Exception inner,
        params string[] resourceArgs)
        : base(null, inner)
    {
        _localizer = localizer;

        _resourceKey = resourceKey;
        _resourceArgs = resourceArgs;
    }

    public override string ToString()
    {
        return _resourceArgs.Length == 0
            ? _localizer.GetString(_resourceKey)
            : _localizer.GetString(_resourceKey, _resourceArgs);
    }
}