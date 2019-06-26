using UnityEngine;

public class CutController : MonoBehaviour {

    private MeshCutter meshCutter = new MeshCutter();

    private void Update() {
        if (Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            var ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out hit)){ Cut(hit.transform.gameObject, transform.up, hit.point); }
        }
    }

    private void Cut(GameObject target, Vector3 normal, Vector3 point) {
        var gos = meshCutter.CutGameObject(target, normal, point);
        
        gos.Item1.AddComponent<Rigidbody>();
        gos.Item1.AddComponent<MeshCollider>();
        gos.Item1.GetComponent<MeshCollider>().convex = true;

        gos.Item2.AddComponent<Rigidbody>();        
        gos.Item2.AddComponent<MeshCollider>();       
        gos.Item2.GetComponent<MeshCollider>().convex = true;
        Destroy(target);
    }
}
