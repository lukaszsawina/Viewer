from pathlib import Path
import cv2

# Ścieżka do folderu, gdzie będą zapisywane obrazy
imagePath = Path('project/images')
imagePath.mkdir(parents=True, exist_ok=True)

# Ścieżka do pliku MP4
video_path = 'test_film.mp4'

# Otwieranie pliku MP4
cap = cv2.VideoCapture(video_path)

frame_no = 0

# Sprawdzenie, czy plik został poprawnie otwarty
if not cap.isOpened():
    print("Nie udało się otworzyć pliku wideo")
else:
    while cap.isOpened():
        ret, frame = cap.read()

        # Jeżeli uda się odczytać klatkę
        if ret:
            # Zapisujemy co 5-tą klatkę
            if frame_no % 5 == 0:
                # Zmniejszenie rozmiaru zdjęcia o połowę
                height, width = frame.shape[:2]
                resized_frame = cv2.resize(frame, (width // 2, height // 2))

                # Ścieżka do zapisu
                target = str(imagePath / f'{frame_no}.jpg')
                cv2.imwrite(target, resized_frame)

            frame_no += 1

            # Zatrzymujemy po 30 minutach wideo (zakładając 30 FPS)
            if frame_no > 60 * 30:
                break
        else:
            # Jeśli nie można odczytać więcej klatek, kończymy pętlę
            break

# Zwolnienie zasobów
cap.release()
