using System.Globalization;
using System.Text;
using Tag.Art;
using TagArena.Movement;

/// <summary>
/// Headless check that SlideBody, PunchStrike, and TagCatch read as those verbs,
/// and that slideBoost is still 0.
/// </summary>
public static class VerbPoseClipProof
{
    public static VerbPoseClipReport Run()
    {
        var report = new VerbPoseClipReport();
        MovementConfig cfg = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
        report.SlideBoost = cfg.slideBoost;
        if (cfg.slideBoost != 0f)
            report.Fail("slideBoost is not 0");

        report.SlideClip = VerbPoseClips.SlideBody;
        report.SlideState = VerbPoseClips.StateSlide;
        report.PunchClip = VerbPoseClips.PunchStrike;
        report.PunchWindupState = VerbPoseClips.StatePunchWindup;
        report.PunchActiveState = VerbPoseClips.StatePunchActive;
        report.TagClip = VerbPoseClips.TagCatch;
        report.TagState = VerbPoseClips.StateTag;

        foreach (string fail in VerbPoseClips.SilhouetteFailures())
            report.Fail(fail);
        return report;
    }
}

public sealed class VerbPoseClipReport
{
    public float SlideBoost;
    public string SlideClip;
    public string SlideState;
    public string PunchClip;
    public string PunchWindupState;
    public string PunchActiveState;
    public string TagClip;
    public string TagState;
    public readonly System.Collections.Generic.List<string> Failures = new System.Collections.Generic.List<string>();

    public bool Ok => Failures.Count == 0;

    public void Fail(string why) => Failures.Add(why);

    public override string ToString()
    {
        var c = CultureInfo.InvariantCulture;
        var text = new StringBuilder();
        text.Append(Ok ? "PASS" : "FAIL");
        text.Append(" clip ").Append(SlideClip).Append(" state ").Append(SlideState);
        text.Append(" | clip ").Append(PunchClip).Append(" states ").Append(PunchWindupState).Append("+").Append(PunchActiveState);
        text.Append(" | clip ").Append(TagClip).Append(" state ").Append(TagState);
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        for (int i = 0; i < Failures.Count; i++)
            text.Append(" | ").Append(Failures[i]);
        return text.ToString();
    }
}
