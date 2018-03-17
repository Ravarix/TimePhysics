---
layout: default
---
# [](#header-1)Simple Lag Compensation for Unity

Based on [industry standard](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking#Lag_compensation) latency compensation mechanics.

![](https://developer.valvesoftware.com/w/images/c/ca/Lag_compensation.jpg)

### [](#header-2)Simple API
Supports all Unity Physics Cast functions, including Sphere/Box/CapsuleCast.

```csharp
void Shoot(Ray ray)
{
    using (TimePhysics.RewindSeconds(Ping))
    {
        RaycastHit hit;
        if (TimePhysics.Raycast(ray, out hit))
        {
            // hit code
        }
    }
}
```

#### [](#header-2)Network Agnostic
There are many different networking solutions in Unity. TimePhysics makes no opinions on which to use, simply supports three kinds of rewinds:

| Method        | Description                                   | Usage                                                |
|:--------------|:----------------------------------------------|:-----------------------------------------------------|
| **RewindSeconds** | `float`: how many seconds to rewind       | Useful for Ping based rewinding                      |
| **RewindFrames**  | `int`: how many Fixed Frames to rewind by | Useful if tracking Player Frame Delay                |
| **RewindToFrame** | `int`: which Fixed Frame to rewind to     | Useful if the frame is encoded in the Player Command |


#### [](#header-2)Zero Allocations
TimePhysics' `using` syntax takes care of restoring transforms without GC allocations.


#### [](#header-2)Highly Performant
Only rewinds the transforms that have a chance of being hit by the cast, greatly reducing the weight on the Physics engine, even when many HitboxBodies are clustered together.

TimePhysics HitboxBodies can also be configured to save at a lower frequency than the fixed time step, and interpolated between. NPCs can easily be set to snapshot 10 times a second (instead of 60) with minimal loss in accuracy.

