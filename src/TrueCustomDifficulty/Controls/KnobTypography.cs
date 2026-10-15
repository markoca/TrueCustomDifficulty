namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Shared Segoe UI typography for the native 270-degree rotary controls.
/// Hero dials may move the caption to clear their larger arc, but use the
/// same title and endpoint font sizes as every other knob.
/// Endpoints are pure white and positioned closer to the dial.
/// </summary>
internal static class KnobTypography
{
    public const double CaptionSize = 10.8;
    public const double EndpointSize = CaptionSize;
    public const double EndpointInsetFromBottom = 25.0;
}
