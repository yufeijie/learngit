function t2 = solveLLC_t2(Vin_, Ts_, wr_, Lm_, ILrp_, fy_)
global Vin Ts wr Lm ILrp fy
Vin = Vin_;
Ts = Ts_;
wr = wr_;
Lm = Lm_;
ILrp = ILrp_;
fy = fy_;
opt = optimset('Display', 'off');
t2 = fsolve(@functionSRC, [Ts/2], opt);
end

function F = functionSRC(x)
global Vin Ts wr Lm ILrp fy
t2 = x(1);
F = Vin/(4*Lm)*(4*t2-Ts)+ILrp*sin(wr*t2+fy);
end