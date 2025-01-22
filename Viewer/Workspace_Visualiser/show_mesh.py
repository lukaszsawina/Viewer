import open3d as o3d
import sys

def show_mesh_with_gui(mesh_path):
    mesh = o3d.io.read_triangle_mesh(mesh_path)

    if not mesh.is_empty():
        vis = o3d.visualization.Visualizer()
        vis.create_window(window_name="Wizualizacja PLY", width=800, height=600)

        vis.add_geometry(mesh)

        vis.run()

        vis.destroy_window()
    else:
        print(f"Error: unable to open file {mesh_path}")

if __name__ == "__main__":
    # Obsługa argumentów z linii poleceń
    if len(sys.argv) != 2:
        print("Użycie: python script.py <mesh_path>")
    else:
        mesh_path = sys.argv[1]
        show_mesh_with_gui(mesh_path)
