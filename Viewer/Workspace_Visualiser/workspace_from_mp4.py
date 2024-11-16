import open3d as o3d
import sys

def show_mesh_with_gui(mesh_path):
    # Wczytanie pliku PLY
    mesh = o3d.io.read_triangle_mesh(mesh_path)

    # Sprawdzenie, czy mesh został załadowany poprawnie
    if not mesh.is_empty():
        # Tworzenie wizualizatora Open3D
        vis = o3d.visualization.Visualizer()
        vis.create_window(window_name="Wizualizacja PLY", width=800, height=600)

        # Dodanie geometrii
        vis.add_geometry(mesh)

        # Uruchomienie wizualizacji
        vis.run()

        # Zamknięcie okna wizualizacji po zakończeniu
        vis.destroy_window()
    else:
        print(f"Błąd: nie udało się wczytać siatki z pliku {mesh_path}")

if __name__ == "__main__":
    # Obsługa argumentów z linii poleceń
    if len(sys.argv) != 2:
        print("Użycie: python script.py <mesh_path>")
    else:
        mesh_path = sys.argv[1]
        show_mesh_with_gui(mesh_path)
