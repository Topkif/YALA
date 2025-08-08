using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using YALA.Models;

namespace YALA.Services;

public class YoloOnnxService
{
	InferenceSession? inferenceSession;
	string[]? labels;
	int inputWidth;
	int inputHeight;
	string? inputName;

	public void LoadOnnxModel(string modelPath)
	{
		inferenceSession?.Dispose();

		var opts = new SessionOptions
		{
			GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
			IntraOpNumThreads = Environment.ProcessorCount,
			InterOpNumThreads = 1
		};

		inferenceSession = new InferenceSession(modelPath, opts);
		inputName = inferenceSession.InputMetadata.Keys.FirstOrDefault();

		ModelMetadata metadata = inferenceSession.ModelMetadata;

		if (metadata.CustomMetadataMap.TryGetValue("names", out var namesRaw))
		{
			namesRaw = namesRaw.Replace("'", "\"").Trim();
			namesRaw = namesRaw.Substring(1, namesRaw.Length - 2);
			var items = namesRaw.Split(", ");
			var dict = new Dictionary<string, string>();
			foreach (var item in items)
			{
				var parts = item.Split(": ", 2);
				if (parts.Length == 2)
				{
					string key = parts[0].Trim().Trim('"');
					string value = parts[1].Trim().Trim('"');
					dict[key] = value;
				}
			}
			labels = dict.OrderBy(kvp => int.Parse(kvp.Key)).Select(kvp => kvp.Value).ToArray();
		}

		if (metadata.CustomMetadataMap.TryGetValue("imgsz", out var sizeRaw))
		{
			sizeRaw = sizeRaw.Trim('[', ']');
			var parts = sizeRaw.Split(',');
			if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
			{
				inputWidth = w;
				inputHeight = h;
			}
		}
	}

	public List<Detection> Detect(string imagePath, double iouThreshold = 0.45, double confThreshold = 0.25)
	{
		try
		{
			using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);
			int imageWidth = image.Width;
			int imageHeight = image.Height;

			// Preprocessing: Resize to model input size
			image.Mutate(x => x.Resize(inputWidth, inputHeight));

			// Flatten image to CHW float array normalized to [0,1]
			float[] inputData = new float[1 * 3 * inputHeight * inputWidth];
			int hw = inputHeight * inputWidth;

			for (int y = 0; y < inputHeight; y++)
			{
				for (int x = 0; x < inputWidth; x++)
				{
					var p = image[x, y];
					int idx = y * inputWidth + x;
					inputData[idx] = p.R / 255f;
					inputData[hw + idx] = p.G / 255f;
					inputData[2 * hw + idx] = p.B / 255f;
				}
			}

			var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 3, inputHeight, inputWidth });
			var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName!, inputTensor) };

			// Inference
			using var results = inferenceSession!.Run(inputs);
			var outputTensor = results.First().AsTensor<float>();

			return Postprocess(outputTensor, imageWidth, imageHeight, iouThreshold,confThreshold);
		}
		catch
		{
			return new List<Detection>();
		}
	}

	private List<Detection> Postprocess(Tensor<float> output, int imageWidth, int imageHeight, double iouThreshold, double confThreshold)
	{
		// Postprocessing: parse output and apply NMS
		var detections = new List<Detection>();
		if (labels == null) return detections;

		int numDetections = output.Dimensions[2];
		int numClasses = labels.Length;

		for (int classId = 0; classId < numClasses; classId++)
		{
			var classDetections = new List<Detection>();

			for (int detIdx = 0; detIdx < numDetections; detIdx++)
			{
				float conf = output[0, 4 + classId, detIdx];
				if (conf < confThreshold) continue;

				var det = new Detection
				{
					xCenter = output[0, 0, detIdx] / inputWidth * imageWidth,
					yCenter = output[0, 1, detIdx] / inputHeight * imageHeight,
					width = output[0, 2, detIdx] / inputWidth * imageWidth,
					height = output[0, 3, detIdx] / inputHeight * imageHeight,
					confidence = conf,
					classId = classId,
					label = labels[classId]
				};

				bool shouldAdd = true;
				var toRemove = new List<int>();

				for (int i = 0; i < classDetections.Count; i++)
				{
					float iou = classDetections[i].CalculateIoU(det);
					if (iou >= iouThreshold)
					{
						if (det.confidence > classDetections[i].confidence)
							toRemove.Add(i);
						else
						{
							shouldAdd = false;
							break;
						}
					}
				}

				foreach (var idx in toRemove.OrderByDescending(i => i)) classDetections.RemoveAt(idx);

				if (shouldAdd) classDetections.Add(det);
			}
			detections.AddRange(classDetections);
		}

		return detections;
	}
}
