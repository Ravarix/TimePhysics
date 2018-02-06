using Unitilities;
using UnityEngine;

namespace Hitbox{
    
[RequireComponent(typeof(HitboxBody))]
public class HitboxBodyDebug : MonoBehaviour
{
    public enum DebugType { Selected, Always }
    
    [SerializeField] private DebugType _debugType;
    [SerializeField] private int _frameDelay = 30;
    [SerializeField] private Color _hitboxColor = new Color(0, 1, 0, 0.5F);
    [SerializeField] private Color _proximityColor = new Color(0, 0, 1, 0.15F);
    private HitboxBody _body;
    private HitboxBody body => _body ?? (_body = GetComponent<HitboxBody>());
    private HitboxMarkerDebug[] _hitboxMarkersDebug;
    
    private void Start()
    {
        _hitboxMarkersDebug = new HitboxMarkerDebug[body.Transforms.Length];
        for (int i = 0; i < body.Transforms.Length; i++)
        {
            var go = body.Transforms[i].gameObject;
            _hitboxMarkersDebug[i] = go.GetComponent<HitboxMarkerDebug>() 
                                     ?? go.AddComponent<HitboxMarkerDebug>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(_debugType == DebugType.Selected)
            DrawGizmos();
    }
    
    private void OnDrawGizmos()
    {
        if(_debugType == DebugType.Always)
            DrawGizmos();
    }

    private void DrawGizmos()
    {
        if (!Application.isPlaying) //don't draw unless we're playing
            return;
        
        var frame = Mathf.Max(body.CurrentSnapshotFrame - _frameDelay, 0);
        if (!TimePhysics.IsFrameValid(frame))
            return;

        if (frame % body.SnapshotFrequency == 0) //if we are on a snapshot
        {
            var index = frame % TimePhysics.NumSnapshots;
            var snapShot = body.Snapshots[index];
            
            for(int hitboxIndex = 0; hitboxIndex < body.Transforms.Length; hitboxIndex++)
                DrawMarkerGizmo(
                    snapShot.LocalToWorld[hitboxIndex], 
                    _hitboxMarkersDebug[hitboxIndex], 
                    _hitboxColor);
            
            var proxSnapshot = body.Snapshots[index].ProximityBounds;
            Gizmos.color = _proximityColor;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(proxSnapshot.center, proxSnapshot.size);
        } else { // lerp
            int index1, index2;
            var lerpVal = body.LerpFrame(frame, out index1, out index2);
           
            var snapShot1 = body.Snapshots[index1];
            var snapShot2 = body.Snapshots[index2];

            for (int hitboxIndex = 0; hitboxIndex < body.Transforms.Length; hitboxIndex++)
                DrawMarkerGizmo(
                    MatrixUtils.LerpMatrixTR(
                        ref snapShot1.LocalToWorld[hitboxIndex], 
                        ref snapShot2.LocalToWorld[hitboxIndex], 
                        lerpVal,
                        body.Transforms[hitboxIndex].lossyScale),
                    _hitboxMarkersDebug[hitboxIndex], _hitboxColor);
            
            var proxSnapshot1 = body.Snapshots[index1].ProximityBounds;
            var proxSnapshot2 = body.Snapshots[index2].ProximityBounds;
            var lerpBounds = MatrixUtils.LerpBounds(ref proxSnapshot1, ref proxSnapshot2, lerpVal);
            
            Gizmos.color = _proximityColor;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(lerpBounds.center, lerpBounds.size);
                
        }
    }
    
    public static void DrawMarkerGizmo(Matrix4x4 matrix, HitboxMarkerDebug markerDebug, Color color, bool wire = true)
    {
        Gizmos.color = color;
        Gizmos.matrix = matrix;
            
        switch (markerDebug.shape)
        {
            case HitboxMarkerDebug.Shape.Box:
                if (wire)
                    Gizmos.DrawWireCube(markerDebug.BoxCollider.center, markerDebug.BoxCollider.size);
                else
                    Gizmos.DrawCube(markerDebug.BoxCollider.center, markerDebug.BoxCollider.size);
                break;
            case HitboxMarkerDebug.Shape.Sphere:
                if (wire)
                    Gizmos.DrawWireSphere(markerDebug.SphereCollider.center, markerDebug.SphereCollider.radius);
                else
                    Gizmos.DrawSphere(markerDebug.SphereCollider.center, markerDebug.SphereCollider.radius);
                break;
        }
    }
}
}