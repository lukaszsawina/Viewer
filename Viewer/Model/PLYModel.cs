using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Viewer.Model;

public class PLYModel
{
    public GeometryModel3D GeometryModel { get; private set; }

    public void LoadPLY(string filePath)
    {
        List<Point3D> vertices = new List<Point3D>();
        List<int> indices = new List<int>();

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // Sprawdzamy nagłówek pliku
            string line = ReadLine(reader);
            if (line != "ply")
                throw new Exception("To nie jest plik PLY!");

            bool isBinary = false;
            int vertexCount = 0;
            int faceCount = 0;

            // Odczytujemy nagłówek
            while (line != "end_header")
            {
                if (line.StartsWith("format"))
                {
                    isBinary = line.Contains("binary");
                }
                else if (line.StartsWith("element vertex"))
                {
                    vertexCount = int.Parse(line.Split(' ')[2]);
                }
                else if (line.StartsWith("element face"))
                {
                    faceCount = int.Parse(line.Split(' ')[2]);
                }
                line = ReadLine(reader);
            }

            if (isBinary)
            {
                // Odczytujemy wierzchołki (6 zmiennych dla każdego wierzchołka)
                for (int i = 0; i < vertexCount; i++)
                {
                    double x = reader.ReadDouble();
                    double y = reader.ReadDouble();
                    double z = reader.ReadDouble();
                    double nx = reader.ReadDouble();
                    double ny = reader.ReadDouble();
                    double nz = reader.ReadDouble();
                    byte r = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte b = reader.ReadByte();

                    // Dodajemy wierzchołek do listy
                    vertices.Add(new Point3D(x, y, z));
                }

                // Odczytujemy twarze (zakładając, że każda twarz to trójkąt)
                for (int i = 0; i < faceCount; i++)
                {
                    byte vertexCountForFace = reader.ReadByte(); // Odczytujemy liczbę wierzchołków w twarzy
                    if (vertexCountForFace == 3) // Tylko trójkąty
                    {
                        int v1 = reader.ReadInt32();
                        int v2 = reader.ReadInt32();
                        int v3 = reader.ReadInt32();
                        indices.Add(v1);
                        indices.Add(v2);
                        indices.Add(v3);
                    }
                }
            }
            else
            {
                throw new Exception("Format binarny pliku nie jest obsługiwany.");
            }
        }

        // Tworzymy MeshGeometry3D z wierzchołków i indeksów
        MeshGeometry3D mesh = new MeshGeometry3D
        {
            Positions = new Point3DCollection(vertices),
            TriangleIndices = new Int32Collection(indices)
        };

        // Tworzymy GeometryModel3D, aby wyświetlić w HelixViewport3D
        GeometryModel = new GeometryModel3D
        {
            Geometry = mesh,
            Material = new DiffuseMaterial(Brushes.Gray)  // Ustawiamy materiał (można zmienić na bardziej zaawansowany)
        };
    }

    // Pomocnicza metoda do odczytu linii nagłówka
    private string ReadLine(BinaryReader reader)
    {
        var line = new System.Text.StringBuilder();
        char currentChar;
        while ((currentChar = reader.ReadChar()) != '\n')
        {
            line.Append(currentChar);
        }
        return line.ToString().Trim();
    }
}

