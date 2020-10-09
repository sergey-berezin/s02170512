# s02170512
Repo for practical .NET programming 2020 tasks (student account s02170512)
 
# NNModel
_____
Kласс -  ONNX-модели со свойствам и методами: 
- путь к модели _(*.onnx)_ 
- путь к меткам классов _(*.txt)_
- размер обрабатываемых изображений
- _mode_  ```if grayscale: mode == true, else rgb: mode == false```
- путь к default-директории с изображениями
- метод MakePrediction, принимающий путь к директории с изображениями для распознования, с параллельным вычислением результата. ```output: ConcurrentQueue``` 
- метод PreprocessImage преобразующий изображение в тензор 
- метод ProcessImage обработки одного изображения 

Нужные модель, метки классов следует положить в директорую с проектом. На данный момент репозиторий содержит MNIST-модель для распознования цифр.

# Workflow
____
Для работы необходимо создать instance класса NNModel, передав в конструктор параметры модели. Затем вызвать в отдельном потоке метод MakePrediction с параметром ```path to directoty with images```, дождаться его завершения, вывести результат на экран

