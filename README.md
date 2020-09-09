# Crack_UWP

A UWP application for crack segmentation

The CrackSegNet downloaded from https://github.com/Arenops/CrackSegNet
Transfered to ONNX model by keras2onnx

# kEYPOINTS:

1. When transfer the Keras model "CrackNet" to ONNX model, use keras2onnx model and set target_opset=7 (Otherwise winML cannot read the model correctly)
2. Inside app, the SoftwareBitmap is transfered to TensorFloat as Input and trans reversly as output
3. When debug, use x64, or instead, the program will crash due to memory shit bomb (about 2GB, waiting to be optimized)