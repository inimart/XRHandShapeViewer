using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using System.Collections.Generic;

public class XRHandShapeViewerWindow : EditorWindow
{
    // References to necessary objects
    private XRHandShape selectedHandShape;
    private GameObject previewHandModel;
    private GameObject defaultHandModel;
    private FromXRHandShapeToMesh handShapeApplier;
    
    // Variables for the preview
    private PreviewRenderUtility previewRenderUtility;
    private Vector2 drag;
    private float zoom = 1.0f;
    private Vector2 panOffset = Vector2.zero; // Offset for panning
    
    // Default and target hand model prefabs
    private GameObject handModelPrefab;
    private GameObject defaultHandModelPrefab;
    
    // References to meshes for the preview
    private List<MeshData> previewMeshes = new List<MeshData>();
    
    // Variables for animation
    private float animationTime = 0f;
    private float animationSpeed = 1f;
    private bool isAnimating = true;
    
    // Structure to store mesh data
    private class MeshData
    {
        public Mesh mesh;
        public Material material;
        public Matrix4x4 matrix;
    }

    [MenuItem("Window/XR/Hand Shape Preview")]
    public static void ShowWindow()
    {
        GetWindow<XRHandShapeViewerWindow>("Hand Shape Preview");
    }

    private void OnEnable()
    {
        // Initialize the preview render utility
        if (previewRenderUtility == null)
        {
            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.cameraFieldOfView = 30.0f;
            
            // Set the camera clip planes to avoid clipping during zoom
            previewRenderUtility.camera.nearClipPlane = 0.01f;
            previewRenderUtility.camera.farClipPlane = 10f;
        }
        
        // Load hand prefabs if necessary
        if (handModelPrefab == null)
        {
            handModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/XRHandShapeViewer/Prefabs/HandModel.prefab");
        }
        
        if (defaultHandModelPrefab == null)
        {
            defaultHandModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/XRHandShapeViewer/Prefabs/DefaultHandModel.prefab");
        }
        
        // Create the preview hand
        CreatePreviewHand();
        
        // Register the callback for selection in the project window
        Selection.selectionChanged += OnSelectionChanged;
        
        // Start continuous update for animation
        EditorApplication.update += OnEditorUpdate;
        
        // Register the callback for the scene
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // Clean up resources
        if (previewRenderUtility != null)
        {
            previewRenderUtility.Cleanup();
            previewRenderUtility = null;
        }
        
        // Destroy preview objects
        if (previewHandModel != null)
        {
            DestroyImmediate(previewHandModel);
            previewHandModel = null;
        }
        
        if (defaultHandModel != null)
        {
            DestroyImmediate(defaultHandModel);
            defaultHandModel = null;
        }
        
        // Remove callbacks
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    // This method is called during scene rendering
    private void OnSceneGUI(SceneView sceneView)
    {
        // Ensure preview objects are not visible in the scene
        if (previewHandModel != null)
        {
            previewHandModel.hideFlags = HideFlags.HideAndDontSave;
            
            // Completely disable the GameObject
            if (previewHandModel.activeSelf)
                previewHandModel.SetActive(false);
        }
        
        if (defaultHandModel != null)
        {
            defaultHandModel.hideFlags = HideFlags.HideAndDontSave;
            
            // Completely disable the GameObject
            if (defaultHandModel.activeSelf)
                defaultHandModel.SetActive(false);
        }
    }

    private void OnEditorUpdate()
    {
        if (isAnimating && selectedHandShape != null)
        {
            // Update animation time
            animationTime += Time.deltaTime * animationSpeed;
            
            // Calculate interpolation factor using a sinusoidal function (0-1-0)
            float t = (Mathf.Sin(animationTime * Mathf.PI) + 1f) * 0.5f;
            
            // Apply animation
            ApplyAnimatedShape(t);
            
            // Force window repaint
            Repaint();
        }
    }

    private void OnSelectionChanged()
    {
        // Check if an XRHandShape has been selected
        if (Selection.activeObject is XRHandShape)
        {
            selectedHandShape = Selection.activeObject as XRHandShape;
            
            // Reset animation
            animationTime = 0f;
            
            // Automatically apply the shape when a new XRHandShape is selected
            ApplyHandShape();
            
            Repaint(); // Force window repaint
        }
    }

    private void CreatePreviewHand()
    {
        // Destroy previous hand if it exists
        if (previewHandModel != null)
        {
            DestroyImmediate(previewHandModel);
            previewHandModel = null;
        }
        
        if (defaultHandModel != null)
        {
            DestroyImmediate(defaultHandModel);
            defaultHandModel = null;
        }
        
        // Create a new instance of the preview hand
        if (handModelPrefab != null)
        {
            // Create the model in a temporary scene to avoid it appearing in the main scene
            previewHandModel = Instantiate(handModelPrefab);
            
            // Completely hide the object from the scene and hierarchy
            previewHandModel.hideFlags = HideFlags.HideAndDontSave;
            
            // Completely disable the GameObject
            previewHandModel.SetActive(false);
            
            // Temporarily reactivate only for configuration
            previewHandModel.SetActive(true);
            
            // Disable all renderers in the scene
            Renderer[] renderers = previewHandModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
                
                // If it's a SkinnedMeshRenderer, also disable bone updates
                if (renderer is SkinnedMeshRenderer skinnedMesh)
                {
                    skinnedMesh.updateWhenOffscreen = false;
                }
            }
            
            // Add the FromXRHandShapeToMesh component
            handShapeApplier = previewHandModel.AddComponent<FromXRHandShapeToMesh>();
            
            // Configure the component
            handShapeApplier.WristRoot_Target = previewHandModel.transform;
            
            // Create and configure the default hand
            if (defaultHandModelPrefab != null)
            {
                defaultHandModel = Instantiate(defaultHandModelPrefab);
                defaultHandModel.hideFlags = HideFlags.HideAndDontSave;
                defaultHandModel.SetActive(false);
                defaultHandModel.SetActive(true);
                
                // Disable all renderers in the scene
                Renderer[] defaultRenderers = defaultHandModel.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in defaultRenderers)
                {
                    renderer.enabled = false;
                    
                    // If it's a SkinnedMeshRenderer, also disable bone updates
                    if (renderer is SkinnedMeshRenderer skinnedMesh)
                    {
                        skinnedMesh.updateWhenOffscreen = false;
                    }
                }
                
                handShapeApplier.WristRoot_Default = defaultHandModel.transform;
            }
            
            // Prepare meshes for the preview
            UpdatePreviewMeshes();
            
            // Disable the GameObjects again after configuration
            previewHandModel.SetActive(false);
            if (defaultHandModel != null)
                defaultHandModel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Hand model prefab not found. Please assign a hand model prefab in the XRHandShapePreviewWindow script.");
        }
    }

    private void UpdatePreviewMeshes()
    {
        // Clear the mesh list
        previewMeshes.Clear();
        
        if (previewHandModel == null) return;
        
        // Temporarily reactivate to extract meshes
        bool wasActive = previewHandModel.activeSelf;
        if (!wasActive)
            previewHandModel.SetActive(true);
        
        // Find all renderers in the hierarchy
        Renderer[] renderers = previewHandModel.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in renderers)
        {
            // Check if it's a SkinnedMeshRenderer
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                Mesh bakedMesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(bakedMesh);
                
                MeshData meshData = new MeshData
                {
                    mesh = bakedMesh,
                    material = skinnedMeshRenderer.sharedMaterial,
                    matrix = skinnedMeshRenderer.transform.localToWorldMatrix
                };
                
                previewMeshes.Add(meshData);
            }
            // Check if it's a MeshRenderer with MeshFilter
            else if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshData meshData = new MeshData
                    {
                        mesh = meshFilter.sharedMesh,
                        material = renderer.sharedMaterial,
                        matrix = renderer.transform.localToWorldMatrix
                    };
                    
                    previewMeshes.Add(meshData);
                }
            }
        }
        
        // Restore previous state
        if (!wasActive)
            previewHandModel.SetActive(false);
    }

    private void OnGUI()
    {
        // Show a message if no XRHandShape is selected
        if (selectedHandShape == null)
        {
            EditorGUILayout.HelpBox("Select an XRHandShape in the Project Window to view the preview.", MessageType.Info);
            return;
        }
        
        // Show information about the selected XRHandShape
        EditorGUILayout.LabelField("Selected XRHandShape:", selectedHandShape.name);
        
        // Controls for animation
        EditorGUILayout.BeginHorizontal();
        
        // Toggle to enable/disable animation
        bool newAnimating = EditorGUILayout.Toggle("Animation", isAnimating);
        if (newAnimating != isAnimating)
        {
            isAnimating = newAnimating;
            if (!isAnimating)
            {
                // If animation is disabled, restore the original shape
                ApplyHandShape();
            }
        }
        
        // Slider for animation speed
        animationSpeed = EditorGUILayout.Slider("Speed", animationSpeed, 0.1f, 3.0f);
        
        EditorGUILayout.EndHorizontal();
        
        // Instructions for controls
        EditorGUILayout.HelpBox(
            "Controls:\n" +
            "- Drag with left mouse button: Rotate the model\n" +
            "- Drag with middle mouse button: Pan the model\n" +
            "- Mouse wheel: Zoom in/out", 
            MessageType.Info);
        
        // Button to reset the view
        if (GUILayout.Button("Reset View"))
        {
            drag = Vector2.zero;
            panOffset = Vector2.zero;
            zoom = 1.0f;
            Repaint();
        }
        
        // Draw the 3D preview
        Rect previewRect = GUILayoutUtility.GetRect(10, 10000, 200, 10000);
        DrawPreview(previewRect);
    }

    private void DrawPreview(Rect rect)
    {
        if (Event.current.type == EventType.Repaint && previewHandModel != null)
        {
            // Configure the preview camera
            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            
            // Position the camera
            previewRenderUtility.camera.transform.position = new Vector3(panOffset.x, panOffset.y, -2.0f * zoom);
            previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            
            // Add light
            previewRenderUtility.lights[0].intensity = 1.0f;
            previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0);
            previewRenderUtility.lights[1].intensity = 0.7f;
            
            // Draw all meshes in the preview
            foreach (MeshData meshData in previewMeshes)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(drag.y, drag.x, 0), Vector3.one);
                previewRenderUtility.DrawMesh(
                    meshData.mesh,
                    rotationMatrix,
                    meshData.material,
                    0
                );
            }
            
            // Render the preview
            previewRenderUtility.camera.Render();
            
            // Draw the preview
            previewRenderUtility.EndAndDrawPreview(rect);
        }
        
        // Handle input for rotation and zoom
        HandlePreviewInput(rect);
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event evt = Event.current;
        
        if (rect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.MouseDrag)
            {
                if (evt.button == 0) // Left mouse button
                {
                    // Rotation - inverted
                    drag.x -= evt.delta.x;
                    drag.y += evt.delta.y;
                    evt.Use();
                    Repaint();
                }
                else if (evt.button == 2) // Middle mouse button
                {
                    // Panning
                    float panSpeed = 0.01f * zoom; // Panning speed proportional to zoom
                    panOffset.x += evt.delta.x * panSpeed;
                    panOffset.y -= evt.delta.y * panSpeed;
                    evt.Use();
                    Repaint();
                }
            }
            else if (evt.type == EventType.ScrollWheel)
            {
                // Zoom - inverted for more natural behavior
                zoom -= evt.delta.y * 0.05f;
                zoom = Mathf.Clamp(zoom, 0.2f, 3.0f);
                evt.Use();
                Repaint();
            }
        }
    }

    private void ApplyHandShape()
    {
        if (handShapeApplier != null && selectedHandShape != null)
        {
            // Temporarily reactivate to apply the shape
            bool wasActive = previewHandModel.activeSelf;
            if (!wasActive)
                previewHandModel.SetActive(true);
            
            // Assign the selected XRHandShape
            handShapeApplier.XRHShape = selectedHandShape;
            
            // Apply the shape
            handShapeApplier.ReadShape();
            
            // Update meshes for the preview
            UpdatePreviewMeshes();
            
            // Restore previous state
            if (!wasActive)
                previewHandModel.SetActive(false);
            
            // Force repaint
            Repaint();
        }
    }
    
    private void ApplyAnimatedShape(float t)
    {
        if (handShapeApplier != null && selectedHandShape != null)
        {
            // Temporarily reactivate to apply the shape
            bool wasActive = previewHandModel.activeSelf;
            if (!wasActive)
                previewHandModel.SetActive(true);
            
            // Assign the selected XRHandShape
            handShapeApplier.XRHShape = selectedHandShape;
            
            // Temporarily modify the ReadShape method to use interpolated values
            ApplyInterpolatedShape(t);
            
            // Update meshes for the preview
            UpdatePreviewMeshes();
            
            // Restore previous state
            if (!wasActive)
                previewHandModel.SetActive(false);
        }
    }
    
    private void ApplyInterpolatedShape(float t)
    {
        if (handShapeApplier == null || selectedHandShape == null) return;
        
        // Access hand joints
        var fingerShapeConditions = selectedHandShape.fingerShapeConditions;
        
        foreach (var condition in fingerShapeConditions)
        {
            // Get joints associated with this condition
            var fingerJoints = handShapeApplier.GetType().GetMethod("GetFingerJoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(handShapeApplier, new object[] { condition.fingerID }) as Dictionary<string, Transform>;
            
            if (fingerJoints == null || fingerJoints.Count == 0) continue;
            
            // Apply rotations for each finger joint
            foreach (var target in condition.targets)
            {
                // Calculate interpolated value between lower and upper limit
                float minValue = target.desired - target.lowerTolerance;
                float maxValue = target.desired + target.upperTolerance;
                float interpolatedValue = Mathf.Lerp(minValue, maxValue, t);
                
                // Clamp value between 0 and 1
                interpolatedValue = Mathf.Clamp01(interpolatedValue);
                
                Transform proximal = fingerJoints["Proximal"];
                Transform intermediate = fingerJoints.ContainsKey("Intermediate") ? fingerJoints["Intermediate"] : null;
                Transform distal = fingerJoints["Distal"];
                
                switch (target.shapeType)
                {
                    case XRFingerShapeType.FullCurl:
                        // Interpolate between 0° (straight) and 90° (bent)
                        float fullCurlAngle = interpolatedValue * 90f;
                        ApplyRotationToJoint(proximal, fullCurlAngle, 0);
                        if (intermediate != null)
                            ApplyRotationToJoint(intermediate, fullCurlAngle, 0);
                        ApplyRotationToJoint(distal, fullCurlAngle, 0);
                        break;
                        
                    case XRFingerShapeType.BaseCurl:
                        // Interpolate between 0° (straight) and 90° (bent) only for proximal
                        float baseCurlAngle = interpolatedValue * 90f;
                        ApplyRotationToJoint(proximal, baseCurlAngle, 0);
                        break;
                        
                    case XRFingerShapeType.TipCurl:
                        // Interpolate between 0° (straight) and 90° (bent) only for distal
                        float tipCurlAngle = interpolatedValue * 90f;
                        ApplyRotationToJoint(distal, tipCurlAngle, 0);
                        break;
                        
                    case XRFingerShapeType.Pinch:
                        // For pinch, rotate the finger towards the thumb
                        float pinchAngle = interpolatedValue * 45f;
                        ApplyRotationToJoint(proximal, pinchAngle, 0);
                        if (intermediate != null)
                            ApplyRotationToJoint(intermediate, pinchAngle, 0);
                        ApplyRotationToJoint(distal, pinchAngle, 0);
                        break;
                        
                    case XRFingerShapeType.Spread:
                        // For spread, rotate the proximal on the Y axis
                        if (condition.fingerID != XRHandFingerID.Little)
                        {
                            float spreadAngle = interpolatedValue * 20f;
                            ApplyRotationToJoint(proximal, 0, spreadAngle);
                        }
                        break;
                }
            }
        }
    }
    
    private void ApplyRotationToJoint(Transform joint, float xAngle, float yAngle)
    {
        if (joint == null) return;
        
        Vector3 currentRotation = joint.localRotation.eulerAngles;
        
        // Apply only specified angles, keeping other values
        if (xAngle != 0)
            currentRotation.x = xAngle;
        if (yAngle != 0)
            currentRotation.y = yAngle;
            
        joint.localRotation = Quaternion.Euler(currentRotation);
    }
} 