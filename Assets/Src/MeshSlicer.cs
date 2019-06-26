using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshSlicer {
    public (GameObject, GameObject) CutGameObject(GameObject target, Vector3 normal, Vector3 point) {
        Plane cuttingPlane = new Plane(target.transform.InverseTransformDirection(normal), 
                                        target.transform.InverseTransformPoint(point));

        Mesh mesh = target.GetComponent<MeshFilter>().mesh;
        Material material = target.GetComponent<MeshRenderer>().material;
        var meshes = CutMesh(mesh, cuttingPlane);
        
        GameObject h1 = new GameObject(target.name + "_part");
        h1.transform.position = target.transform.position;
        h1.transform.rotation = target.transform.rotation;
        h1.transform.localScale = target.transform.localScale;
        h1.AddComponent<MeshFilter>();
        h1.AddComponent<MeshRenderer>();
        h1.GetComponent<MeshFilter>().mesh = meshes.Item1;
        h1.GetComponent<MeshRenderer>().material = material;
        
        GameObject h2 = new GameObject(target.name + "_part");
        h2.transform.position = target.transform.position;
        h2.transform.rotation = target.transform.rotation;
        h2.transform.localScale = target.transform.localScale;
        h2.AddComponent<MeshFilter>();
        h2.AddComponent<MeshRenderer>();
        h2.GetComponent<MeshFilter>().mesh = meshes.Item2;
        h2.GetComponent<MeshRenderer>().material = material;

        return (h1, h2);
    }

    private (Mesh, Mesh) CutMesh(Mesh mesh, Plane cuttingPlane) {
        Mesh m1 = new Mesh();
        Mesh m2 = new Mesh();

        List<Vector3> front_verts = new List<Vector3>();
        List<Vector2> front_uvs = new List<Vector2>();
        List<Vector3> front_normals = new List<Vector3>();
        List<int> front_tris = new List<int>();

        List<Vector3> back_verts = new List<Vector3>();
        List<Vector2> back_uvs = new List<Vector2>();
        List<Vector3> back_normals = new List<Vector3>();
        List<int> back_tris = new List<int>();

        Dictionary<Vector3, Vector3> hull_edges = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < mesh.triangles.Count(); i += 3) {
            var halves = CutTriangle(new Vector3[]{ mesh.vertices[mesh.triangles[i]],
                                                    mesh.vertices[mesh.triangles[i+1]],
                                                    mesh.vertices[mesh.triangles[i+2]] }, 
                                     new Vector2[]{ mesh.uv[mesh.triangles[i]],
                                                    mesh.uv[mesh.triangles[i+1]],
                                                    mesh.uv[mesh.triangles[i+2]] },
                                     new Vector3[]{ mesh.normals[mesh.triangles[i]],
                                                    mesh.normals[mesh.triangles[i+1]],
                                                    mesh.normals[mesh.triangles[i+2]] },                                   
                                                    front_verts.Count(), back_verts.Count(), cuttingPlane);
            
            front_verts.AddRange(halves.Item1.Item1);
            front_uvs.AddRange(halves.Item1.Item2);
            front_normals.AddRange(halves.Item1.Item3);
            front_tris.AddRange(halves.Item1.Item4);

            back_verts.AddRange(halves.Item2.Item1);
            back_uvs.AddRange(halves.Item2.Item2);
            back_normals.AddRange(halves.Item2.Item3);
            back_tris.AddRange(halves.Item2.Item4);

            if (!hull_edges.ContainsKey(halves.Item3.Item1) && halves.Item3.Item1 != halves.Item3.Item2) { 
                hull_edges.Add(halves.Item3.Item1, halves.Item3.Item2); 
            }
        }

        List<List<Vector3>> hulls = GetHullLoops(hull_edges);
        for (int i = 0; i < hulls.Count(); i++) {
            var hull = TriangulateHull(cuttingPlane.normal, hulls[i], front_verts.Count(), back_verts.Count());
            front_verts.AddRange(hull.Item1);
            front_uvs.AddRange(hull.Item2);
            front_normals.AddRange(hull.Item3);
            front_tris.AddRange(hull.Item5);

            back_verts.AddRange(hull.Item1);
            back_uvs.AddRange(hull.Item2);
            back_normals.AddRange(hull.Item4);
            back_tris.AddRange(hull.Item6);
        }

        m1.vertices = front_verts.ToArray();
        m1.triangles = front_tris.ToArray();
        m1.uv = front_uvs.ToArray();
        m1.normals = front_normals.ToArray();

        m2.vertices = back_verts.ToArray();
        m2.triangles = back_tris.ToArray();
        m2.uv = back_uvs.ToArray();
        m2.normals = back_normals.ToArray();

        return (m1, m2);
    }

    private ((Vector3[], Vector2[], Vector3[], int[]), (Vector3[], Vector2[], Vector3[], int[]), (Vector3, Vector3)) CutTriangle(Vector3[] tri, Vector2[] uvs, Vector3[] normals, 
                                                                                                                                 int front_idx, int back_idx, Plane cuttingPlane) {
        List<Vector3> front = new List<Vector3>();
        List<Vector3> back = new List<Vector3>();

        List<Vector2> front_uvs = new List<Vector2>();
        List<Vector2> back_uvs = new List<Vector2>();

        List<Vector3> front_normals = new List<Vector3>();
        List<Vector3> back_normals = new List<Vector3>();

        List<int> front_indices = new List<int>(){front_idx+0,front_idx+1,front_idx+2};
        List<int> back_indices = new List<int>(){back_idx+0,back_idx+1,back_idx+2};

        (Vector3, Vector3) hull_edge = (Vector3.zero, Vector3.zero);

        for (int i = 0; i < 3; i++) {
            int i0 = (2+i)%3;

            Vector3 p1 = tri[i0];
            Vector3 p2 = tri[i];     

            Vector2 u1 = uvs[i0];
            Vector2 u2 = uvs[i];

            Vector3 n1 = normals[i0];
            Vector3 n2 = normals[i];

            float d1 = cuttingPlane.GetDistanceToPoint(p1);
            float d2 = cuttingPlane.GetDistanceToPoint(p2);

            if (d2 > float.Epsilon){
                if (d1 < -float.Epsilon) {
                    float disp = d2/(Mathf.Abs(d1)+Mathf.Abs(d2));
                    Vector3 p2p1 = Vector3.Lerp(p2, p1, disp);
                    Vector2 u2u1 = Vector2.Lerp(u2, u1, disp);
                    Vector3 n2n1 = Vector3.Lerp(n2, n1, disp);

                    front.Add(p2p1);
                    back.Add(p2p1);
                    hull_edge.Item1 = p2p1;

                    front_uvs.Add(u2u1);
                    back_uvs.Add(u2u1);

                    front_normals.Add(n2n1);
                    back_normals.Add(n2n1);
                }
                back.Add(p2);
                back_uvs.Add(u2);
                back_normals.Add(n2);
            } else if (d2 < -float.Epsilon) {
                if (d1 > float.Epsilon) {
                    float disp = d1/(Mathf.Abs(d1)+Mathf.Abs(d2));
                    Vector3 p1p2 = Vector3.Lerp(p1, p2, disp);
                    Vector2 u1u2 = Vector2.Lerp(u1, u2, disp);
                    Vector3 n1n2 = Vector3.Lerp(n1, n2, disp);

                    front.Add(p1p2);
                    back.Add(p1p2);
                    hull_edge.Item2 = p1p2;

                    front_uvs.Add(u1u2);
                    back_uvs.Add(u1u2);

                    front_normals.Add(n1n2);
                    back_normals.Add(n1n2);

                } else if (d1 <= float.Epsilon && d1 >= -float.Epsilon) { 
                    front.Add(p1); 
                    front_uvs.Add(u1);
                    front_normals.Add(n1);
                }
                front.Add(p2);
                front_uvs.Add(u2);
                front_normals.Add(n2);

            } else {
                back.Add(p2);
                back_uvs.Add(u2);
                back_normals.Add(n2);
                if (d1 < -float.Epsilon) { 
                    front.Add(p2); 
                    front_uvs.Add(u2);
                    front_normals.Add(n2);
                }
            }
        }

        if (front.Count == 4) { front_indices.AddRange(new int[]{front_idx+3,front_idx+0,front_idx+2}); }
        else if (back.Count == 4) { back_indices.AddRange(new int[]{back_idx+3,back_idx+0,back_idx+2}); }
        if (front.Count < 3) { front.Clear(); front_indices.Clear(); }
        else if (back.Count < 3) { back.Clear(); back_indices.Clear(); }

        return ((front.ToArray(), front_uvs.ToArray(), front_normals.ToArray(), front_indices.ToArray()), 
                (back.ToArray(), back_uvs.ToArray(), back_normals.ToArray(), back_indices.ToArray()),
                hull_edge);
    }

    private List<List<Vector3>> GetHullLoops(Dictionary<Vector3, Vector3> hull_edges) {
        List<List<Vector3>> hulls = new List<List<Vector3>>();
        if (hull_edges.Count() == 0) { return hulls; }

        Vector3 start = hull_edges.First().Key;
        Vector3 next = hull_edges.First().Value;
        hulls.Add(new List<Vector3>(){start});


        while (hull_edges.Count() > 0) {
            Vector3 curr = next;
            if (!hull_edges.ContainsKey(curr)) { break; }
            next = hull_edges[curr];
            if (curr == start) { 
                hull_edges.Remove(start);
                if (hull_edges.Count() == 0) { break; }
                start = hull_edges.First().Key;
                next = hull_edges.First().Value; 
                hulls.Add(new List<Vector3>(){start});
            } else {
                hulls.Last().Add(curr);
                hull_edges.Remove(curr);
            }
        }

        return hulls;
    }

    private (List<Vector3>, List<Vector2>, List<Vector3>, List<Vector3>, List<int>, List<int>) TriangulateHull(Vector3 normal, List<Vector3> hull, int front_idx, int back_idx) {
        Vector3 hull_center = new Vector3();
        List<Vector2> hull_uvs = new List<Vector2>();
        List<Vector3> hull_normals_back = new List<Vector3>();
        List<Vector3> hull_normals_front = new List<Vector3>();
        List<int> hull_tris_back = new List<int>();
        List<int> hull_tris_front = new List<int>();

        for (int i = 0; i < hull.Count(); i++) { 
            hull_center += hull[i];
            hull_tris_front.AddRange(new int[]{ front_idx+hull.Count(), front_idx+i, front_idx+(i+1)%hull.Count() });
            hull_tris_back.AddRange(new int[]{ back_idx+hull.Count(), back_idx+i, back_idx+(i+1)%hull.Count() });
            hull_normals_back.Add(-normal);
            hull_normals_front.Add(normal);
        }

        hull_tris_front.Reverse();
        hull_center /= hull.Count();
        hull.Add(hull_center);       
        hull_normals_back.Add(-normal);
        hull_normals_front.Add(normal);

        for (int i = 0; i < hull.Count(); i++) { 
            Vector3 p = Vector3.ProjectOnPlane(hull[i], normal);
            hull_uvs.Add(new Vector2(p.x, p.y));
        }

        return (hull, hull_uvs, hull_normals_front, hull_normals_back, hull_tris_front, hull_tris_back);
    }
}