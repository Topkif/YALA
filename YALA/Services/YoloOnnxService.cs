using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YALA.Models;

namespace YALA.Services;

public class YoloOnnxService
{
	InferenceSession? inferenceSession;
	string[]? labels;
	int inputWidth;
	int inputHeight;
	float iouThreshold = 0.45f; // Intersection over Union threshold for NMS
	float confThreshold = 0.25f; // Confidence threshold for detections
	public void LoadOnnxModel(string modelPath)
	{
		inferenceSession?.Dispose();
		inferenceSession = new InferenceSession(modelPath);
		ModelMetadata metadata = inferenceSession.ModelMetadata;

		// Parse 'names' metadata
		if (metadata.CustomMetadataMap.TryGetValue("names", out var namesRaw))
		{
			// Convert to valid JSON
			namesRaw = namesRaw.Replace("'", "\"").Trim();
			namesRaw = namesRaw.Substring(1, namesRaw.Length - 2); // remove { and }
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

		// Parse 'imgsz' metadata
		if (metadata.CustomMetadataMap.TryGetValue("imgsz", out var sizeRaw))
		{
			// Assumes format is "[640,640]"
			sizeRaw = sizeRaw.Trim('[', ']');
			var parts = sizeRaw.Split(',');
			if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
			{
				inputWidth = w;
				inputHeight = h;
			}
		}
	}

	public List<Detection> Detect(string imagePath)
	{
		try
		{
		// Preprocess the image
		using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);
		int imageWidth = image.Bounds.Width;
		int imageHeight = image.Bounds.Height;
		image.Mutate(x => x.Resize(inputWidth, inputHeight)); // Resize to model input size
		Tensor<float> inputTensor = new DenseTensor<float>(new[] { 1, 3, inputWidth, inputHeight }); // Create tensor with shape [1, 3, inputSize, inputSize]

		for (int y = 0; y < inputHeight; y++)
		{
			for (int x = 0; x < inputWidth; x++)
			{
				Rgb24 pixel = image[x, y];
				inputTensor[0, 0, y, x] = pixel.R / 255f;
				inputTensor[0, 1, y, x] = pixel.G / 255f;
				inputTensor[0, 2, y, x] = pixel.B / 255f;
			}
		}

			List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
			Tensor<float>? output = inferenceSession?.Run(inputs)?.First().AsTensor<float>();
			if (output != null)
			{
				return Postprocess(output, imageWidth, imageHeight);
			}
			else
			{
				return new List<Detection>();
			}
		}
		catch
		{
			return new List<Detection>();
		}
	}

	private List<Detection> Postprocess(Tensor<float> output, int imageWidth, int imageHeight)
	{
		// [1, (4 + N), 8400] = [batch, (xc yc w h) + Classes confidence, detections]
		List<Detection> detections = new();
		if (labels != null)
		{
			// For each class
			for (int i = 0; i< labels.Length; i++)
			{
				List<Detection> classDetections = new();
				// For each detection
				for (int j = 0; j < output.Dimensions[2]; j++)
				{
					// We consider the detection if it is above the confidence threshold
					if (output[0, 4 + i, j] >= confThreshold)
					{
						Detection newDetection = new Detection
						{
							xCenter = output[0, 0, j] / inputWidth * imageWidth,
							yCenter = output[0, 1, j] / inputHeight * imageHeight,
							width = output[0, 2, j] / inputWidth * imageWidth,
							height = output[0, 3, j] / inputHeight * imageHeight,
							confidence = output[0, 4 + i, j],
							classId = i,
							label = labels[i]
						};

						// If the NMS threshold is not exceeded for every other classDetection members
						bool shouldAdd = true;
						List<int> toRemove = new();
						for (int k = 0; k < classDetections.Count; k++)
						{
							var existing = classDetections[k];
							float iou = existing.CalculateIoU(newDetection);
							if (iou >= iouThreshold) // Same detection
							{
								if (newDetection.confidence > existing.confidence)
								{
									toRemove.Add(k);
								}
								else
								{
									shouldAdd = false;
									break;
								}
							}
						}

						foreach (int idx in toRemove.OrderByDescending(i => i))
						{
							classDetections.RemoveAt(idx); 
						}
						if (shouldAdd)
						{
							classDetections.Add(newDetection);
						}
						if (classDetections.Count == 0) // For the first detection of this class
						{
							classDetections.Add(newDetection); // If no detections, we add the new one
						}
					}
				}
				detections.AddRange(classDetections);
			}
		}
		return detections.Take(Math.Min(detections.Count,10)).ToList();
	}
}
