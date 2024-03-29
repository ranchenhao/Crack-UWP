// This file was automatically generated by VS extension Windows Machine Learning Code Generator v3
// from model file crack.onnx
// Warning: This file may get overwritten if you add add an onnx file with the same name
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
namespace Crack_UWP
{
    
    public sealed class crackInput
    {
        public TensorFloat input_1; // shape(-1,512,512,3)
    }
    
    public sealed class crackOutput
    {
        public TensorFloat conv2d_21; // shape(-1,512,512,1)
    }
    
    public sealed class crackModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<crackModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            crackModel learningModel = new crackModel();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<crackOutput> EvaluateAsync(crackInput input)
        {
            binding.Bind("input_1", input.input_1);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new crackOutput();
            output.conv2d_21 = result.Outputs["conv2d_21"] as TensorFloat;
            return output;
        }
    }
}

