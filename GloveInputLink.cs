using System.IO.Pipes;
using System.Runtime.InteropServices;

/*
 * library created by Rin.
 * discord: Riɳ#0001
 * 
 * This library is for interacting with the openGloves driver.
 * More specifically, it is designed to send glove inputs via named pipes to control the buttons, fingers, and other inputs.
 * 
 * written in c# for those like me who are scared of c++
 * 
 * --------------------Thanks To--------------------
 * 
 * danwillm - He helped a ton with formatting the InputData struct. 
 * I couldn't have done this without him so huge thanks. 
 * 
 * L4rs - This library took heavy inspiration from his c# library for sending force feedback inputs to the glove via named pipes. I would have been very lost without him.
 * https://github.com/Hydr4bytes/OpenGlovesLib - his library linked here.
 * 
 * -------------------------------------------------
 */

namespace GloveInputLib
{
    //Struct InputData is a struct that contains all of the button, finger, and linear inputs. This is what's sent to the driver via the named pipe.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct InputData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public float[] flexion; //range: 0 -> 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public float[] splay; //range: -1 -> 1
        public float joyX; //range: -1 -> 1
        public float joyY; //range: -1 -> 1
        [MarshalAs(UnmanagedType.I1)]
        public bool joyButton;
        [MarshalAs(UnmanagedType.I1)]
        public bool trgButton;
        [MarshalAs(UnmanagedType.I1)]
        public bool aButton;
        [MarshalAs(UnmanagedType.I1)]
        public bool bButton;
        [MarshalAs(UnmanagedType.I1)]
        public bool grab;
        [MarshalAs(UnmanagedType.I1)]
        public bool pinch;
        [MarshalAs(UnmanagedType.I1)]
        public bool menu;
        [MarshalAs(UnmanagedType.I1)]
        public bool calibrate;
        public float trgValue;//range: 0 -> 1

        //constructor that uses a 1d array for flexion.
        public InputData(float[] flexion, float[] splay, float joyX, float joyY, bool joyButton, bool trgButton, bool aButton, bool bButton, bool grab, bool pinch, bool menu, bool calibrate, float trgValue)
        {
            this.flexion = flexion;
            this.splay = splay;
            this.joyX = joyX;
            this.joyY = joyY;
            this.joyButton = joyButton;
            this.trgButton = trgButton;
            this.aButton = aButton;
            this.bButton = bButton;
            this.grab = grab;
            this.pinch = pinch;
            this.menu = menu;
            this.calibrate = calibrate;
            this.trgValue = trgValue;
        }

        //this constructor is for if you want to instead use a 2d array for the flexion. it's an array of 5 arrays of 4 floats. (float[5,4] flexion). each finger is represented by an array. each float in the array represents a joint. 
        public InputData(float[,] flexion, float[] splay, float joyX, float joyY, bool joyButton, bool trgButton, bool aButton, bool bButton, bool grab, bool pinch, bool menu, bool calibrate, float trgValue)
        {
            this.flexion = new float[20];
            //nested for loops to turn the 2d array to a 1d array.
            int flexIndex = 0;
            for (int finger = 0; finger < 5; finger++)
            {
                for (int joint = 0; joint < 4; joint++)
                {
                    this.flexion[flexIndex++] = flexion[finger,joint];
                }
            }
            this.splay = splay;
            this.joyX = joyX;
            this.joyY = joyY;
            this.joyButton = joyButton;
            this.trgButton = trgButton;
            this.aButton = aButton;
            this.bButton = bButton;
            this.grab = grab;
            this.pinch = pinch;
            this.menu = menu;
            this.calibrate = calibrate;
            this.trgValue = trgValue;
        }

        //default constructor that sets all booleon values to false and all float values to 0.
        public InputData()
        {
            flexion = new float[20] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            splay = new float[5] { 0, 0, 0, 0, 0 };
            joyX = 0;
            joyY = 0;
            joyButton = false;
            trgButton = false;
            aButton = false;
            bButton = false;
            grab = false;
            pinch = false;
            menu = false;
            calibrate = false;
            trgValue = 0;
        }
    }


    //The GloveInputLink class is used for actually sending inputs to 
    public class GloveInputLink
    {
        private NamedPipeClientStream pipe;

        //Handness is an enum used to pass what hand the GloveInputLink object will write to
        public enum Handness
        {
            Left,
            Right
        }

        //Constructor. takes handness parameter to tell it to write to specific hand.
        public GloveInputLink(Handness handness)
        {
            string hand = handness == Handness.Right ? "right" : "left";
            pipe = new NamedPipeClientStream(".", $"vrapplication\\input\\glove\\v2\\{hand}", PipeDirection.Out);
            //connect to the pipe
            Console.WriteLine($"Connecting to {hand} hand pipe...");
            try
            {
                //try to connect to the pipe, timeout after 5 seconds
                pipe.Connect(5000);
            }
            catch (Exception e)
            {
                //if an error is thrown log the message
                Console.WriteLine(e.Message);
            }
            if (pipe.IsConnected)
                Console.WriteLine($"Connected! CanWrite:{pipe.CanWrite}");
            else
                Console.WriteLine("Connection failed");
        }

        //set all input values to default.
        public void Relax()
        {
            Write(new InputData());
        }

        //send values to the driver.
        public void Write(InputData input)
        {
            if (!pipe.IsConnected) return;

            int size = Marshal.SizeOf(input);
            Console.WriteLine(size);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            pipe.Write(arr, 0, size);
        }
    }
}
