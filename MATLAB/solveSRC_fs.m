function fs = solveSRC_fs(Q_, Vin_, n_, t0_, fr_)
global Q Vin n t0 fr wr
Q = Q_;
Vin = Vin_;
n = n_;
t0 = t0_;
fr = fr_;
wr = 2*pi*fr_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, [Vin/n fr*1.01], opt);
fs = x(2);
end

function F = functionSRC(x)
global Q Vin n t0 fr wr
Vo = x(1); fs = x(2);
F = [(n*pi*Q*Vo/2*fr/fs+Vin-n*Vo)*sin(wr*(t0-1/(2*fs)))+(n*pi*Q*Vo/2*fr/fs+Vin+n*Vo)*sin(wr*t0);
     (n*pi*Q*Vo/2*fr/fs+Vin-n*Vo)*cos(wr*(t0-1/(2*fs)))+(n*pi*Q*Vo/2*fr/fs+Vin+n*Vo)*cos(wr*t0)-2*Vin];
end