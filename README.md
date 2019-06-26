# simpleslice
A realtime mesh slicer for the game engine Unity3d, capable of dynamically separating concave objects into multiple parts.

## how to use
Simply add the **MeshCutter.cs** to your project.
The code below shows you how to separate a rigidbody into two rigidbodies.

```c#
private MeshSlicer meshSlicer = new MeshSlicer();

private void Update() {
    if (Input.GetMouseButtonDown(0)){
        RaycastHit hit;
        var ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out hit)){ 
            Cut(hit.transform.gameObject, transform.up, hit.point); 
        }
    }
}

private void Cut(GameObject target, Vector3 normal, Vector3 point) {
    var gos = meshSlicer.CutGameObject(target, normal, point);

    gos.Item1.AddComponent<Rigidbody>();
    gos.Item1.AddComponent<MeshCollider>();
    gos.Item1.GetComponent<MeshCollider>().convex = true;

    gos.Item2.AddComponent<Rigidbody>();        
    gos.Item2.AddComponent<MeshCollider>();       
    gos.Item2.GetComponent<MeshCollider>().convex = true;
    Destroy(target);
}
```

## todo
- [x] Calculate UVs for cap
- [x] Cut concave objects
- [ ] Assign n > 2 objects to their own gameobjects 

## example
![slice_img](https://github.com/olafvisker/simpleslice/blob/master/Img/slice.png "Sliced Cube")
