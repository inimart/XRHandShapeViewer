using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using System.Collections.Generic;

public class FromXRHandShapeToMesh : MonoBehaviour
{
    public XRHandShape XRHShape;
    
    [Header("Hand Roots")]
    public Transform WristRoot_Target;  // Root transform of the hand to be modified
    public Transform WristRoot_Default; // Root transform of the hand with default pose

    private Dictionary<string, Transform> targetJoints = new Dictionary<string, Transform>();
    private Dictionary<string, Quaternion> defaultRotations = new Dictionary<string, Quaternion>();

    // Names of the joints we expect to find in the hierarchy
    private readonly string[] jointNames = new string[]
    {
        "Wrist",
        "ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip",
        "IndexMetacarpal", "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip",
        "MiddleMetacarpal", "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip",
        "RingMetacarpal", "RingProximal", "RingIntermediate", "RingDistal", "RingTip",
        "LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip"
    };

    void OnEnable()
    {
        // Initialization can also be done in edit mode
    }

    [ContextMenu("Read Shape")]
    public void ReadShape()
    {
        if (XRHShape == null)
        {
            Debug.LogError("XRHandShape not assigned!");
            return;
        }

        if (WristRoot_Target == null)
        {
            Debug.LogError("WristRoot_Target not assigned!");
            return;
        }

        if (WristRoot_Default == null)
        {
            Debug.LogError("WristRoot_Default not assigned!");
            return;
        }

        // Get references to joints and default rotations
        GetHandJointTransforms();

        // Reset all joints to default rotations
        ResetAllJointsRotation();

        // Apply rotations based on shape conditions
        foreach (var condition in XRHShape.fingerShapeConditions)
        {
            // Get joints associated with this condition
            var fingerJoints = GetFingerJoints(condition.fingerID);
            if (fingerJoints == null || fingerJoints.Count == 0) continue;

            // Apply rotations for each finger joint
            foreach (var target in condition.targets)
            {
                float desiredValue = target.desired;
                Transform proximal = fingerJoints["Proximal"];
                Transform intermediate = fingerJoints.ContainsKey("Intermediate") ? fingerJoints["Intermediate"] : null;
                Transform distal = fingerJoints["Distal"];
                Transform tip = fingerJoints["Tip"];

                switch (target.shapeType)
                {
                    case XRFingerShapeType.FullCurl:
                        // Interpolate between 0° (straight) and 90° (bent)
                        float fullCurlAngle = Mathf.Lerp(0f, 90f, desiredValue);
                        ApplyRotationX(proximal, fullCurlAngle);
                        if (intermediate != null)
                            ApplyRotationX(intermediate, fullCurlAngle);
                        ApplyRotationX(distal, fullCurlAngle);
                        break;

                    case XRFingerShapeType.BaseCurl:
                        // Interpolate between 0° (straight) and 90° (bent) only for proximal
                        float baseCurlAngle = Mathf.Lerp(0f, 90f, desiredValue);
                        ApplyRotationX(proximal, baseCurlAngle);
                        break;

                    case XRFingerShapeType.TipCurl:
                        // Interpolate between 0° (straight) and 90° (bent) only for distal
                        float tipCurlAngle = Mathf.Lerp(0f, 90f, desiredValue);
                        ApplyRotationX(distal, tipCurlAngle);
                        break;

                    case XRFingerShapeType.Pinch:
                        // For pinch, rotate the finger towards the thumb
                        // The rotation angle depends on the finger
                        float pinchAngle = Mathf.Lerp(0f, 45f, desiredValue);
                        ApplyRotationX(proximal, pinchAngle);
                        if (intermediate != null)
                            ApplyRotationX(intermediate, pinchAngle);
                        ApplyRotationX(distal, pinchAngle);
                        break;

                    case XRFingerShapeType.Spread:
                        // For spread, rotate the proximal on the Y axis
                        // Ignore spread for the little finger
                        if (condition.fingerID != XRHandFingerID.Little)
                        {
                            float spreadAngle = Mathf.Lerp(0f, 20f, desiredValue);
                            ApplyRotationY(proximal, spreadAngle);
                        }
                        break;
                }
            }
        }
    }

    // Get references to all hand joints and default rotations
    private void GetHandJointTransforms()
    {
        // Clear dictionaries
        targetJoints.Clear();
        defaultRotations.Clear();

        // Find all joints in the hierarchy of WristRoot_Target
        FindJointsInHierarchy(WristRoot_Target, targetJoints);

        // Find all joints in the hierarchy of WristRoot_Default and save their rotations
        Dictionary<string, Transform> defaultJoints = new Dictionary<string, Transform>();
        FindJointsInHierarchy(WristRoot_Default, defaultJoints);

        // Save default rotations
        foreach (var joint in defaultJoints)
        {
            defaultRotations[joint.Key] = joint.Value.localRotation;
        }
    }

    // Recursively find all joints in the hierarchy
    private void FindJointsInHierarchy(Transform root, Dictionary<string, Transform> joints)
    {
        // Check if the current transform name matches one of the joint names
        foreach (string jointName in jointNames)
        {
            if (root.name.Contains(jointName))
            {
                joints[jointName] = root;
                break;
            }
        }

        // Recursively search all children
        foreach (Transform child in root)
        {
            FindJointsInHierarchy(child, joints);
        }
    }

    // Reset all joints to default rotations
    private void ResetAllJointsRotation()
    {
        foreach (var joint in targetJoints)
        {
            if (defaultRotations.ContainsKey(joint.Key))
            {
                joint.Value.localRotation = defaultRotations[joint.Key];
            }
        }
    }

    // Apply a rotation on the X axis while maintaining existing rotations on other axes
    private void ApplyRotationX(Transform joint, float angleX)
    {
        if (joint == null) return;
        
        Vector3 currentRotation = joint.localRotation.eulerAngles;
        joint.localRotation = Quaternion.Euler(angleX, currentRotation.y, currentRotation.z);
    }

    // Apply a rotation on the Y axis while maintaining existing rotations on other axes
    private void ApplyRotationY(Transform joint, float angleY)
    {
        if (joint == null) return;
        
        Vector3 currentRotation = joint.localRotation.eulerAngles;
        joint.localRotation = Quaternion.Euler(currentRotation.x, angleY, currentRotation.z);
    }

    // Get joints of a specific finger
    private Dictionary<string, Transform> GetFingerJoints(XRHandFingerID fingerID)
    {
        Dictionary<string, Transform> fingerJoints = new Dictionary<string, Transform>();
        string fingerPrefix = "";

        switch (fingerID)
        {
            case XRHandFingerID.Thumb:
                fingerPrefix = "Thumb";
                break;
            case XRHandFingerID.Index:
                fingerPrefix = "Index";
                break;
            case XRHandFingerID.Middle:
                fingerPrefix = "Middle";
                break;
            case XRHandFingerID.Ring:
                fingerPrefix = "Ring";
                break;
            case XRHandFingerID.Little:
                fingerPrefix = "Little";
                break;
            default:
                return fingerJoints;
        }

        // Find all joints belonging to this finger
        foreach (var joint in targetJoints)
        {
            if (joint.Key.StartsWith(fingerPrefix))
            {
                string jointType = joint.Key.Substring(fingerPrefix.Length);
                fingerJoints[jointType] = joint.Value;
            }
        }

        return fingerJoints;
    }

    void Start()
    {
        // Runtime initialization
    }

    void Update()
    {
        
    }
}
