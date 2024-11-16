import sys
import open3d as o3d
import numpy as np

def save_mesh(input_ply_path, output_ply_path):
    # Wczytanie chmury punktów z pliku .ply
    point_cloud = o3d.io.read_point_cloud(input_ply_path)

    # Próbkowanie chmury punktów
    voxel_size = 0.005
    print("Próbkowanie chmury punktów...")
    point_cloud = point_cloud.voxel_down_sample(voxel_size)

    # Ustawienie wartości depth
    depth = 12

    # Rekonstrukcja powierzchni przy użyciu algorytmu Poissona
    print("Rekonstrukcja powierzchni przy użyciu algorytmu Poissona...")
    poisson_mesh, densities = o3d.geometry.TriangleMesh.create_from_point_cloud_poisson(point_cloud, depth=depth)

    # Filtrowanie na podstawie gęstości
    densities = np.asarray(densities)
    density_threshold = np.percentile(densities, 0.25)
    vertices_to_keep = densities > density_threshold
    poisson_mesh.remove_vertices_by_mask(~vertices_to_keep)

    # Zapisz siatkę do pliku
    o3d.io.write_triangle_mesh(output_ply_path, poisson_mesh)
    print(f"Siatka została zapisana do pliku: {output_ply_path}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Użycie: python script.py <input_ply_path> <output_ply_path>")
    else:
        input_ply_path = sys.argv[1]
        output_ply_path = sys.argv[2]
        save_mesh(input_ply_path, output_ply_path)
