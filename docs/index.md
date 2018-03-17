---
layout: default
---
# [](#header-1)Simple Lag Compensation for Unity

Based on [industry standard](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking#Lag_compensation) latency compensation mechanics.

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

#### [](#header-3)Network Agnostic
There are many different networking solutions in Unity. TimePhysics makes no opinions on which to use, simply supports three kinds of rewinds:

| Method        | Description                                   | Notes                                                |
|:--------------|:----------------------------------------------|:-----------------------------------------------------|
| RewindSeconds | Takes a `float` of how many seconds to rewind | Useful for Ping based rewinding                      |
| RewindFrames  | Takes an int of how many frames to rewind by  | Useful if tracking Player Frame Delay                |
| RewindToFrame | Takes an int of which frame to rewind to      | Useful if the frame is encoded in the Player Command |


#### [](#header-3)Zero Allocations
TimePhysics' `using` syntax takes care of restoring transforms without GC allocations.


#### [](#header-3)Highly Performant
Only rewinds the transforms that have a chance of being hit by the cast, greatly reducing the weight on the Physics engine.


#### [](#header-4)Header 4

*   This is an unordered list following a header.
*   This is an unordered list following a header.
*   This is an unordered list following a header.

##### [](#header-5)Header 5

1.  This is an ordered list following a header.
2.  This is an ordered list following a header.
3.  This is an ordered list following a header.

###### [](#header-6)Header 6

| head1        | head two          | three |
|:-------------|:------------------|:------|
| ok           | good swedish fish | nice  |
| out of stock | good and plenty   | nice  |
| ok           | good `oreos`      | hmm   |
| ok           | good `zoute` drop | yumm  |

### There's a horizontal rule below this.

* * *

### Here is an unordered list:

*   Item foo
*   Item bar
*   Item baz
*   Item zip

### And an ordered list:

1.  Item one
1.  Item two
1.  Item three
1.  Item four

### And a nested list:

- level 1 item
  - level 2 item
  - level 2 item
    - level 3 item
    - level 3 item
- level 1 item
  - level 2 item
  - level 2 item
  - level 2 item
- level 1 item
  - level 2 item
  - level 2 item
- level 1 item

### Small image

![](https://assets-cdn.github.com/images/icons/emoji/octocat.png)

### Large image

![](https://guides.github.com/activities/hello-world/branching.png)


### Definition lists can be used with HTML syntax.

<dl>
<dt>Name</dt>
<dd>Godzilla</dd>
<dt>Born</dt>
<dd>1952</dd>
<dt>Birthplace</dt>
<dd>Japan</dd>
<dt>Color</dt>
<dd>Green</dd>
</dl>

```
Long, single-line code blocks should not wrap. They should horizontally scroll if they are too long. This line should be long enough to demonstrate this.
```

```
The final element.
```