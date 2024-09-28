using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VoxelTerrain
{
    // Example script
    public class BrushAndGUI : MonoBehaviour
    {
        public static BrushAndGUI Instance;

        public RectTransform DropdownMenu;
        public RectTransform SaveSuccessPanel;
        public RectTransform ResetPanel;
        public RectTransform PolyCountPanel;
        public Text BrushSizeSlider;

        public Transform HighlighterSphere;
        public Transform HighlighterCube;
        public Transform HighlighterPlane;
        public Transform BoundsVisualizer;

        // set default values
        private float _distance = 100F;
        private Vector3 _position = Vector3.zero;
        public float BrushSize = 13F;
        private bool _paintEnable = false;
        public VoxTerrain.OBJ BrushObject = VoxTerrain.OBJ.SPHERE; //default brush object
        private VoxTerrain.EFFECT BrushEffect = VoxTerrain.EFFECT.ADD;  //default brush effect
        static short grass = 0; //texture 1
        static short stone = 1; //texture 2
        static short sand = 2; //texture 3
        static short white = 3; //White or no texture
        private short paintBrush = 1; // current brush/texture

        private Ray _ray;
        private RaycastHit _hit;
        private float _lastClick = 0;

        private bool doPaint = true;
        private bool doSculpt = true;
        private bool Continuous = false;

        private bool mouseDown = false;
        private float dragCameraSpeed = 0.1f;
        private float rotateCameraSpeed = 5f;
        private float scrollCameraSpeed = 15f;
        private float flyCameraSpeed = 20f;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            //BrushSizeSlider.text = "Brush Size: " + BrushSize;
            BoundsVisualizer.gameObject.SetActive(false);
        }
        
        void OnGUI()
        {
            // switch brushs on the keyboard buttons
            if (Input.GetKeyDown("1"))
            {
                paintBrush = grass;
            }
            if (Input.GetKeyDown("2"))
            {
                paintBrush = stone;
            }
            if (Input.GetKeyDown("3"))
            {
                paintBrush = sand;
            }
            if (Input.GetKeyDown("4"))
            {
                paintBrush = white;
            }
            if(Input.GetKeyDown("5")){
                OnSaveConfirmPressed();
            }
        }
        
        void Update()
        {
            
            // comparation with screen height is only because of down GUI
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && Input.GetKey(KeyCode.LeftControl)) { mouseDown = true; }
            #else // Used to test drawing on mobile...
            if (Input.GetMouseButtonDown(0)&&!EventSystem.current.IsPointerOverGameObject(0)) {mouseDown=true;}
            #endif
            if (Input.GetMouseButtonUp(0)) { mouseDown = false; }
            //*/

            // Get paintPosition
            //*
            _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Find point to paint, if point find
            if (Physics.Raycast(_ray, out _hit))
            {
                // Get distance
                _distance = _hit.distance;
            }
            // Get Contact position
            _position = _ray.GetPoint(_distance);
            //*/
            // Display mesh in paint point
            UpdateHighlighter();
            
            // if in sculpt mode and mouse down
            if (mouseDown)
            {
                // left click, if you decreese 2F time to lower you ger more precision
                if (mouseDown) { if (_lastClick < Time.time - 2F) { _paintEnable = true; } }
                else { _paintEnable = false; }

                // Paint
                if (!_paintEnable)
                    VoxTerrain.Instance.ReBuildCollider();
                    VoxTerrain.Instance.ReBuildColliderClient();

                VoxTerrain.Instance.Draw3D(_position, new Vector3(BrushSize, BrushSize, BrushSize), BrushObject, BrushEffect, paintBrush, doSculpt, doPaint);
                // for continous brush enable this
                if (Continuous)
                {
                    VoxTerrain.Instance.ReBuildCollider();
                    VoxTerrain.Instance.ReBuildColliderClient();
                }

            }

            else
            {
                VoxTerrain.Instance.ReBuildCollider();
                VoxTerrain.Instance.ReBuildColliderClient();
            }

            
            // camera movement section
            if (Input.GetKey(KeyCode.UpArrow)) { Camera.main.transform.Rotate(new Vector3(-1, 0, 0), flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.DownArrow)) { Camera.main.transform.Rotate(new Vector3(1, 0, 0), flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.LeftArrow)) { Camera.main.transform.Rotate(new Vector3(0, -1, 0), flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.RightArrow)) { Camera.main.transform.Rotate(new Vector3(0, 1, 0), flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.W)) { Camera.main.transform.position += Camera.main.transform.transform.TransformDirection(Vector3.forward * flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.S)) { Camera.main.transform.position -= Camera.main.transform.transform.TransformDirection(Vector3.forward * flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.A)) { Camera.main.transform.position += Camera.main.transform.transform.TransformDirection(Vector3.left * flyCameraSpeed * Time.deltaTime); }
            if (Input.GetKey(KeyCode.D)) { Camera.main.transform.position += Camera.main.transform.transform.TransformDirection(Vector3.right * flyCameraSpeed * Time.deltaTime); }

            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!Input.GetKey(KeyCode.LeftControl) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButton(0))
                {
                    Vector3 newPos = new Vector3(-Input.GetAxis("Mouse X") * dragCameraSpeed, -Input.GetAxis("Mouse Y") * dragCameraSpeed, 0) * Camera.main.transform.position.magnitude;
                    Camera.main.transform.Translate(newPos, Space.Self);
                }

                if (Input.GetMouseButton(1))
                {
                    Camera.main.transform.Rotate(0, -Input.GetAxis("Mouse X") * rotateCameraSpeed, 0, Space.World);
                    Camera.main.transform.Rotate(Input.GetAxis("Mouse Y") * rotateCameraSpeed, 0, 0);
                }

                Camera.main.transform.Translate(Camera.main.transform.forward * Input.GetAxis("Mouse ScrollWheel") * scrollCameraSpeed, Space.World);
            }

            if(Input.GetKey(KeyCode.T))
            {
                float height = 5;
                Vector3 pos = new Vector3(VoxTerrain.Instance.width, VoxTerrain.Instance.height, VoxTerrain.Instance.depth) / 2;
                pos.y = height;
                VoxTerrain.Instance.Draw3D(pos, new Vector3(VoxTerrain.Instance.width, height, VoxTerrain.Instance.depth), VoxTerrain.OBJ.CUBE, VoxTerrain.EFFECT.ADD, 0, true, true);
            }
            #endif
            //*/
        }

        private void UpdateHighlighter()
        {
            HighlighterSphere.gameObject.SetActive(false);
            HighlighterCube.gameObject.SetActive(false);
            HighlighterPlane.gameObject.SetActive(false);
            Transform highligher = null;

            float sizeX = BrushSize;
            float sizeY = BrushSize;
            float sizeZ = BrushSize;

            if (BrushObject == VoxTerrain.OBJ.SPHERE)
            {
                highligher = HighlighterSphere;
            }
            else if (BrushObject == VoxTerrain.OBJ.CUBE)
            {
                highligher = HighlighterCube;
            }
            else if (BrushObject == VoxTerrain.OBJ.RANDOM)
            {
                highligher = HighlighterSphere;
                sizeY = HighlighterPlane.transform.localScale.y;
            }
            else if (BrushObject == VoxTerrain.OBJ.PLANE)
            {
                highligher = HighlighterPlane;
                sizeY = HighlighterPlane.transform.localScale.y;
            }

            if (highligher != null)
            {
                highligher.gameObject.SetActive(true);
                highligher.localScale = new Vector3(sizeX, sizeY, sizeZ);
                highligher.position = _position;
            }
        }

        public void ChangeTexture(int textureIndex)
        {
            if (textureIndex == grass) paintBrush = grass;
            if (textureIndex == stone) paintBrush = stone;
            if (textureIndex == sand) paintBrush = sand;
            if (textureIndex == white) paintBrush = white;
        }

        public void SetMode(int modeIndex)
        {
            if (modeIndex == 0) { doSculpt = true; doPaint = false; } // Sculpt Mode
            if (modeIndex == 1) { doSculpt = false; doPaint = true; } // Paint Mode
            if (modeIndex == 2) { doSculpt = true; doPaint = true; } // Combined Mode
        }

        public void SetContinuousMode(int isOn)
        {
            Continuous = (isOn == 1);
        }

        public void SetDrawingShape(int shapeIndex)
        {
            if (shapeIndex == 0) { BrushObject = VoxTerrain.OBJ.CUBE; }
            if (shapeIndex == 1) { BrushObject = VoxTerrain.OBJ.SPHERE; }
            if (shapeIndex == 2) { BrushObject = VoxTerrain.OBJ.RANDOM; }
            if (shapeIndex == 3) { BrushObject = VoxTerrain.OBJ.PLANE; }
        }

        public void SetBrushSize(float sliderSize)
        {
            if (sliderSize > -1) { BrushEffect = VoxTerrain.EFFECT.ADD; BrushSize = sliderSize; }
            else { BrushEffect = VoxTerrain.EFFECT.SUB; BrushSize = -sliderSize; }

            BrushSizeSlider.text = "Brush Size: " + sliderSize;
        }

        public void OnMenuPressed()
        {
            DropdownMenu.gameObject.SetActive(true);
        }

        public void OnMenuItemSelected(int itemIndex)
        {
            if (itemIndex == 0) OnSaveConfirmPressed(); // On Save button pressed
            if (itemIndex == 1) ResetPanel.gameObject.SetActive(true); // On Reset button pressed
            if (itemIndex == 2) OnShowPolyCountPressed(); // On Show Poly Count button pressed

            OnItemSelectionDone();
        }

        public void OnItemSelectionDone()
        {
            DropdownMenu.gameObject.SetActive(false);
        }

        public void OnResetConfirmed()
        {
            VoxTerrain.Instance.ResetMap(); // SceneManager.LoadScene("VoxelTerrainScene");
            VoxTerrain.Instance.ReBuildCollider();
            VoxTerrain.Instance.ReBuildColliderClient();
        }

        public void OnSaveConfirmPressed()
        {
            print("Saving " + VoxTerrain.Instance.TerrainName);
            VoxTerrain.SaveToFile(VoxTerrain.Instance, VoxTerrain.Instance.TerrainName);

            SaveSuccessPanel.gameObject.SetActive(true);
        }

        public void OnLoadTerrainPressed(Dropdown selectTerrainDropdown)
        {
            print("Loading");
            VoxTerrain.LoadFromFile(selectTerrainDropdown.captionText.text);
        }

        public void OnShowPolyCountPressed()
        {
            PolyCountPanel.gameObject.SetActive(true);

            int polyCount = 0;
            foreach (VoxelTerrain.Cube c in VoxTerrain.Instance._cubes)
            {
                polyCount += c.mesh.triangles.Length / 3;
            }

            PolyCountPanel.Find("Text PolyCount").GetComponent<Text>().text = "Poly Count: " + polyCount;
        }

        public void OnShowHideBoundsPressed()
        {
            BoundsVisualizer.gameObject.SetActive(!BoundsVisualizer.gameObject.activeSelf);

            if (BoundsVisualizer.gameObject.activeSelf)
            {
                Vector3 boundsSize = new Vector3(VoxTerrain.Instance.width, VoxTerrain.Instance.height, VoxTerrain.Instance.depth);
                BoundsVisualizer.transform.position = boundsSize / 2;
                BoundsVisualizer.transform.localScale = boundsSize;
            }
        }
    }
}
