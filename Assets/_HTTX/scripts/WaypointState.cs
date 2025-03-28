public class WaypointState
{
    public Waypoints waypoints;
    public int activeChildLength;
    public bool isMovingForward;
    public bool canLoop;
    public bool isWalking;
    public bool isRunning;
    public bool isSitting;
    public bool sameOld;

    public WaypointState(Waypoints waypoints, int activeChildLength, bool isMovingForward, bool canLoop, bool isWalking, bool isRunning, bool isSitting, bool sameOld)
    {
        this.waypoints = waypoints;
        this.activeChildLength = activeChildLength;
        this.isMovingForward = isMovingForward;
        this.canLoop = canLoop;
        this.isWalking = isWalking;
        this.isRunning = isRunning;
        this.isSitting = isSitting;
        this.sameOld = sameOld;
    }

    public void updateAnimator(bool isWalking, bool isRunning, bool isSitting){
        this.isWalking = isWalking;
        this.isRunning = isRunning;
        this.isSitting = isSitting;
    }
}