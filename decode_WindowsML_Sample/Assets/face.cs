using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// aa35781f-f307-4695-8d91-0a53270eb066_113fcd3b-8b9f-43bb-918e-726e5eb62695

namespace decode_WindowsML_Sample
{
    public sealed class Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>()
            {
                { "chomado", float.NaN },
                { "deployprince", float.NaN },
            };
        }
    }

    public sealed class Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695Model
    {
        private LearningModelPreview learningModel;
        public static async Task<Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695Model> CreateAa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695Model(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695Model model = new Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695Model();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelOutput> EvaluateAsync(Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelInput input) {
            Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelOutput output = new Aa35781f_x002D_f307_x002D_4695_x002D_8d91_x002D_0a53270eb066_113fcd3b_x002D_8b9f_x002D_43bb_x002D_918e_x002D_726e5eb62695ModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
