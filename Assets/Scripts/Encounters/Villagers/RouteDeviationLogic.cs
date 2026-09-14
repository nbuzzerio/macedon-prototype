namespace Macedon.Villagers
{
    public enum RouteDeviationStage { OnRoute, WaitingForWarning1, Warning1, Warning2, Abandon }

    public sealed class RouteDeviationState
    {
        public float Elapsed { get; private set; }
        public RouteDeviationStage Stage { get; private set; } = RouteDeviationStage.OnRoute;

        public RouteDeviationStage Update(bool eligible, bool onRoute, float deltaTime, float warning1, float warning2, float abandon)
        {
            if (!eligible || onRoute) { Reset(); return Stage; }
            Elapsed += deltaTime < 0f ? 0f : deltaTime;
            float second = warning1 + warning2;
            float final = second + abandon;
            RouteDeviationStage next = Elapsed >= final ? RouteDeviationStage.Abandon
                : Elapsed >= second ? RouteDeviationStage.Warning2
                : Elapsed >= warning1 ? RouteDeviationStage.Warning1
                : RouteDeviationStage.WaitingForWarning1;
            RouteDeviationStage previous = Stage;
            Stage = next;
            return next != previous ? next : RouteDeviationStage.WaitingForWarning1;
        }

        public void Reset() { Elapsed = 0f; Stage = RouteDeviationStage.OnRoute; }
    }
}
