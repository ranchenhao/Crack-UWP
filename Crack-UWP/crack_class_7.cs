// This file was automatically generated by VS extension Windows Machine Learning Code Generator v3
// from model file crack_class_7.onnx
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
    
    public sealed class crack_class_7Input
    {
        public TensorFloat input_1; // shape(-1,672,672,3)
    }
    
    public sealed class crack_class_7Output
    {
        public TensorFloat flatten; // shape(-1,9)
    }
    
    public sealed class crack_class_7Model
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<crack_class_7Model> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            crack_class_7Model learningModel = new crack_class_7Model();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<crack_class_7Output> EvaluateAsync(crack_class_7Input input)
        {
            binding.Bind("input_1", input.input_1);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new crack_class_7Output();
            output.flatten = result.Outputs["flatten"] as TensorFloat;
            return output;
        }
    }
}
