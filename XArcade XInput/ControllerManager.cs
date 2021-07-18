using ScpDriverInterface;

namespace XArcade_XInput {
    class ControllerManagerEventArgs : System.EventArgs {
        public int Index;
        public X360Controller Controller;
    }

    public enum X360Axis {
        LeftTrigger,
        RightTrigger,
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY,
    }

    class ControllerManager {
        ScpBus Bus;
        public bool IsRunning = false;
        public event System.EventHandler<ControllerManagerEventArgs> OnChange;
        // bool DidEmulateGuide = false;
        bool SOCDlr = false;
        bool SOCDdu = false;

        X360Controller[] controllers = new X360Controller[] {
            new X360Controller(),
            new X360Controller(),
            new X360Controller(),
            new X360Controller(),
        };

        public ControllerManager () {
            Bus = new ScpBus();
        }

        public void Start () {
            if (IsRunning) {
                return;
            }

            IsRunning = true;
            controllers = new X360Controller[] {
                new X360Controller(),
                new X360Controller(),
                new X360Controller(),
                new X360Controller(),
            };

            UnplugAll();

            foreach (var index in Program.KeyboardMapperInstance.MappedControllerIndexes) {
                Bus.PlugIn(index + 1);
            }

            System.Console.WriteLine("Started ControllerManager");
        }

        public void Stop () {
            IsRunning = false;
            UnplugAll();
            System.Console.WriteLine("Stopped ControllerManager");
        }

        public void UnplugAll () {
            // Reset all inputs
            foreach (var index in Program.KeyboardMapperInstance.MappedControllerIndexes) {
                Bus.Report(index + 1, new X360Controller().GetReport());
            }

            System.Threading.Thread.Sleep(250);
            Bus.UnplugAll();
            System.Threading.Thread.Sleep(250);
        }

        public void ButtonDown (int Index, X360Buttons Button) {
            if (!IsRunning) {
                return;
            }

            var current = controllers[Index];
            var next = new X360Controller(current);

            next.Buttons |= Button;

            // // RB + Start = Guide
            // if (next.Buttons.HasFlag(X360Buttons.RightBumper | X360Buttons.Start)) {
            //     next.Buttons &= ~(X360Buttons.RightBumper | X360Buttons.Start);
            //     next.Buttons |= X360Buttons.Logo;
            //     DidEmulateGuide = true;
            // }

            // SOCD Cleaner : left+right
            if (SOCDlr | next.Buttons.HasFlag(X360Buttons.Left | X360Buttons.Right)) {
                next.Buttons &= ~(X360Buttons.Left | X360Buttons.Right);
                SOCDlr = true;
            }

            // SOCD Cleaner : down+up
            if (SOCDdu | next.Buttons.HasFlag(X360Buttons.Down | X360Buttons.Up)) {
                next.Buttons &= ~(X360Buttons.Down);
                SOCDdu = true;
            }

            if (current.Buttons == next.Buttons) {
                return;
            }

            controllers[Index] = next;
            InvokeChange(Index);
            if (Program.IsDebug) {
                System.Console.WriteLine($"Controller #{Index + 1} {Button} Button Down");
            }
        }

        public void ButtonUp (int Index, X360Buttons Button) {
            if (!IsRunning) {
                return;
            }

            var current = controllers[Index];
            var next = new X360Controller(current);
            next.Buttons &= ~Button;

            // if (DidEmulateGuide && Button == X360Buttons.RightBumper || Button == X360Buttons.Start) {
            //     next.Buttons &= ~(X360Buttons.Logo | X360Buttons.RightBumper | X360Buttons.Start);
            //     DidEmulateGuide = false;
            // }

            // SOCD Cleaner : left+right
            if (SOCDlr && (Button == X360Buttons.Left || Button == X360Buttons.Right)) {
                SOCDlr = false;
                next.Buttons |= (Button == X360Buttons.Left) ? X360Buttons.Right : X360Buttons.Left;
            }

            // SOCD Cleaner : down+up
            if (SOCDdu && (Button == X360Buttons.Down || Button == X360Buttons.Up)) {
                SOCDdu = false;
                next.Buttons |= (Button == X360Buttons.Down) ? X360Buttons.Up : X360Buttons.Down;
            }

            if (current.Buttons == next.Buttons) {
                return;
            }

            controllers[Index] = next;
            InvokeChange(Index);
            if (Program.IsDebug) {
                System.Console.WriteLine($"Controller #{Index + 1} {Button} Button Up");
            }
        }

        public void SetAxis (int Index, X360Axis Axis, int Value) {
            if (!IsRunning) {
                return;
            }

            int current = int.MinValue;
            var controller = controllers[Index];

            switch (Axis) {
                case X360Axis.LeftTrigger:
                    current = controller.LeftTrigger;
                    break;
                case X360Axis.RightTrigger:
                    current = controller.RightTrigger;
                    break;
                case X360Axis.LeftStickX:
                    current = controller.LeftStickX;
                    break;
                case X360Axis.LeftStickY:
                    current = controller.LeftStickY;
                    break;
                case X360Axis.RightStickX:
                    current = controller.RightStickX;
                    break;
                case X360Axis.RightStickY:
                    current = controller.RightStickY;
                    break;
            }

            if (current == int.MinValue) {
                return;
            }

            if (current == Value) {
                return;
            }

            switch (Axis) {
                case X360Axis.LeftTrigger:
                    controller.LeftTrigger = (byte)Value;
                    break;
                case X360Axis.RightTrigger:
                    controller.RightTrigger = (byte)Value;
                    break;
                case X360Axis.LeftStickX:
                    controller.LeftStickX = (short)Value;
                    break;
                case X360Axis.LeftStickY:
                    controller.LeftStickY = (short)Value;
                    break;
                case X360Axis.RightStickX:
                    controller.RightStickX = (short)Value;
                    break;
                case X360Axis.RightStickY:
                    controller.RightStickY = (short)Value;
                    break;
            }

            InvokeChange(Index);
            if (Program.IsDebug) {
                System.Console.WriteLine($"Controller #{Index + 1} {Axis} {Value}");
            }
        }

        void InvokeChange (int Index) {
            Bus.Report(Index + 1, controllers[Index].GetReport());

            OnChange?.Invoke(null, new ControllerManagerEventArgs {
                Index = Index,
                Controller = controllers[Index],
            });
        }
    }
}
