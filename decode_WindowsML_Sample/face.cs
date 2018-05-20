using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// 47c46b62-55d4-418f-9667-bca58c105516_df5a0f61-d714-4789-8077-27f227995765

namespace decode_WindowsML_Sample
{
    public sealed class _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>()
            {
                { "chomado", float.NaN },
                { "deployprince", float.NaN },
                { "linyixian", float.NaN },
            };
        }
    }

    public sealed class _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765Model
    {
        private LearningModelPreview learningModel;
        public static async Task<_x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765Model> Create_x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765Model(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765Model model = new _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765Model();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<_x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelOutput> EvaluateAsync(_x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelInput input) {
            _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelOutput output = new _x0034_7c46b62_x002D_55d4_x002D_418f_x002D_9667_x002D_bca58c105516_df5a0f61_x002D_d714_x002D_4789_x002D_8077_x002D_27f227995765ModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
