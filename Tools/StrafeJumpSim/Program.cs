using System;

static class Program
{
    static int Main()
    {
        StrafeJumpReport report = StrafeJumpProof.Run60();
        Console.WriteLine(report.ToString());
        if (!report.Ok)
        {
            Console.Error.WriteLine(report.FailureText);
            return 1;
        }

        GrappleReport grapple = GrappleProof.Run60();
        Console.WriteLine(grapple.ToString());
        if (!grapple.Ok)
        {
            Console.Error.WriteLine(grapple.FailureText);
            return 1;
        }

        return 0;
    }
}
