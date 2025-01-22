import sys
import open3d as o3d
import numpy as np

def save_mesh(input_ply_path, output_ply_path):
    point_cloud = o3d.io.read_point_cloud(input_ply_path)

    voxel_size = 0.002
    point_cloud = point_cloud.voxel_down_sample(voxel_size)

    depth = 12
    poisson_mesh, densities = o3d.geometry.TriangleMesh.create_from_point_cloud_poisson(point_cloud, depth=depth)

    densities = np.asarray(densities)
    density_threshold = np.percentile(densities, 1.5)
    vertices_to_keep = densities > density_threshold
    poisson_mesh.remove_vertices_by_mask(~vertices_to_keep)

    o3d.io.write_triangle_mesh(output_ply_path, poisson_mesh)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Użycie: python script.py <input_ply_path> <output_ply_path>")
    else:
        input_ply_path = sys.argv[1]
        output_ply_path = sys.argv[2]
        save_mesh(input_ply_path, output_ply_path)
