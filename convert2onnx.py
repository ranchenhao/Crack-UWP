from model import *
import numpy as np
import skimage.transform as trans
from skimage import io
import matplotlib.pyplot as plt

import onnxruntime
import keras2onnx

# Load model 
model_name = 'ImprovedFCN'
model = unet3s2()
model.load_weights(model_name + '.hdf5')

# Convert to onnx model and save
version = 7
onnx_model = keras2onnx.convert_keras(model, model.name, target_opset=version)
keras2onnx.save_model(onnx_model, 'crack_'+ str(version) +'.onnx')


# Test model
test_img = "7001-40.jpg"
rgb_img = io.imread(test_img)
#img = color.rgb2gray(rgb_img)
img = rgb_img
img = trans.resize(img, (512, 512, 3), mode='edge')
img = np.reshape(img, (1,) + img.shape)
img = img.astype(np.float32)

# Predict
content = onnx_model.SerializeToString()
sess = onnxruntime.InferenceSession(content)
output_name = sess.get_outputs()[0].name
input_name = sess.get_inputs()[0].name
seg_img = sess.run([output_name], {input_name: img})

seg_img = np.squeeze(seg_img)
seg_img[seg_img <= 0.5] = 0
seg_img[seg_img > 0.5] = 1

# plot segment and mask
implot = plt.imshow(seg_img)
plt.show()
