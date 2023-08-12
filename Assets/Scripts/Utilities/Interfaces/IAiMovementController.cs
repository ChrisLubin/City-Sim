public interface IAiMovementController
{
    Direction UpcomingDirection { get; }

    public void SetIsAtRedLightOrStopSign(bool isAtRedLightOrStopSign);
    public void SetIsAtIntersection(bool isAtIntersection);
    public void SetHasRightOfWay(bool hasRightOfWay);
}
