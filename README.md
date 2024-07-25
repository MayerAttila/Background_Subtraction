# background-subtraction
## Intirudction
In the solution of the program, I used C# and the EmguCV library to implement the ViBe algorithm. EmguCV is a .NET wrapper for the OpenCV image processing library, which allows calling certain OpenCV functions in .NET-compatible languages such as C#.
## Loading the video and extracting frames
After starting the program, a visual interface welcomes us, where we have the option to open a file using the *"ToolStripMenu"*. Once we select the video file we want to open, the program saves it in a variable of type *"VideoCapture"* and then calculates the total number of frames in the video. The frame to be displayed is saved in a variable named *"currentFrameMatRgb"*. The frames are displayed in a "*PictureBox"* and for this, the program converts the stored frames to *"Bitmap"* type.
## Creating a model for each pixel
The program can create two separate models: one for black-and-white video and another for color video. The model is initialized using the **InitalizeBcModellRgb()** and **InitalizeBcModellGray()** functions. "In both cases, I use two nested for loops to iterate through all the pixels in the image. For each pixel, the program takes 20 samples. During sampling, the program randomly select a pixel from the neighborhood of the pixel being examined and store its values in a list. Once sampling is complete, the program places these values into the model. This process is repeated for each pixel.
## ViBe Algorithm 
In my program, I have created two types of ViBe algorithms: one for analyzing black-and-white videos and another for analyzing color videos. Their operation is almost identical. In both cases, two nested for loops are employed to inspect the frame pixel by pixel. Since every pixel already has a background model, the program compare the value of the pixel being examined with the values in the background model. After this comparison, the program can determine whether the pixel is part of the foreground or the background. During runtime, the program continuously updates the background models associated with the pixels, striving for self-improvement.

**Video Demo:**