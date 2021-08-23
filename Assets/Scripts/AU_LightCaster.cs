using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AU_LightCaster : MonoBehaviour {

    // Objects to ignore
    [SerializeField] LayerMask walls;

    // The object for the map walls. (Create this in Blender as one object and put it in the scene with tag "Wall".)
    GameObject mapObject;

    // The offset that you want to change the raycasts by
    private float offset = 0.01f;

    [SerializeField] GameObject lightRays;

    private Mesh mesh;

    // Start is called before the first frame update
    void Start() {
        this.mesh = lightRays.GetComponent<MeshFilter>().mesh;
        this.mapObject = GameObject.FindWithTag("Wall");
    }

    // Update is called once per frame
    void Update() {
        this.mesh.Clear();

        SortedList<float, Vector3> anglesAndVerticies = new SortedList<float, Vector3>();
        Queue<Vector3> verts = new Queue<Vector3>();
        Queue<Vector2> uvs = new Queue<Vector2>();

        verts.Enqueue(lightRays.transform.worldToLocalMatrix.MultiplyPoint3x4(this.transform.position));
        uvs.Enqueue(new Vector2(verts.Peek().x + .5f, verts.Peek().y + .5f));

        Vector3 myLocation = this.transform.position;

        Vector3[] mapMesh = mapObject.GetComponent<MeshFilter>().mesh.vertices;
        for(int i = 0; i < mapMesh.Length; i++) {
            Vector3 verticalLocation = mapObject.transform.localToWorldMatrix.MultiplyPoint3x4(mapMesh[i]);
            RaycastHit hitL, hitR;

            float angleL = Mathf.Atan2((verticalLocation.y - myLocation.y - offset), (verticalLocation.x - myLocation.x - offset));
            float angleR = Mathf.Atan2((verticalLocation.y - myLocation.y + offset), (verticalLocation.x - myLocation.x + offset));

            Physics.Raycast(myLocation, new Vector2(verticalLocation.x - myLocation.x - offset, verticalLocation.y - myLocation.y - offset), out hitL, 100, walls);
            Physics.Raycast(myLocation, new Vector2(verticalLocation.x - myLocation.x + offset, verticalLocation.y - myLocation.y + offset), out hitR, 100, walls);
            Debug.DrawLine(myLocation, hitL.point, Color.red);
            Debug.DrawLine(myLocation, hitR.point, Color.green);
            
            if(!anglesAndVerticies.ContainsKey(angleL)) {
                Vector3 verticyL = lightRays.transform.worldToLocalMatrix.MultiplyPoint3x4(hitL.point);
                anglesAndVerticies.Add(angleL, verticyL);
            }

            if(!anglesAndVerticies.ContainsKey(angleR)) {
                Vector3 verticyR = lightRays.transform.worldToLocalMatrix.MultiplyPoint3x4(hitR.point);
                anglesAndVerticies.Add(angleR, verticyR);
            }
        }


        foreach(Vector3 vertiBoi in anglesAndVerticies.Values) {
            verts.Enqueue(vertiBoi);
            uvs.Enqueue(new Vector2(vertiBoi.x + .5f, vertiBoi.y + .5f));
        }
        this.mesh.vertices = verts.ToArray();
        this.mesh.uv = uvs.ToArray();

        Queue<int> triangles = new Queue<int>();
        triangles.Enqueue(0);
        triangles.Enqueue(1);
        triangles.Enqueue(verts.Count - 1);
        for(int i = verts.Count-1; i > 0; i--) {
            triangles.Enqueue(0);
            triangles.Enqueue(i);
            triangles.Enqueue(i-1);
        }
        this.mesh.triangles = triangles.ToArray();
        Physics2D.SyncTransforms();
    }
}
