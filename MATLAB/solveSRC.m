function x = solveSRC(Q_, Vin_, n_, fs_, fr_)
global Q Vin n fs fr wr Ts
Q = Q_;
Vin = Vin_;
n = n_;
fs = fs_;
fr = fr_;
wr = 2*pi*fr_;
Ts = 1/fs_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, [Vin/n 0], opt);
end

function F = functionSRC(x)
global Q Vin n fs fr wr Ts
Vo = x(1); t0 = x(2);
F = [(n*pi*Q*Vo/2*fr/fs+Vin-n*Vo)*sin(wr*(t0-Ts/2))+(n*pi*Q*Vo/2*fr/fs+Vin+n*Vo)*sin(wr*t0);
     (n*pi*Q*Vo/2*fr/fs+Vin-n*Vo)*cos(wr*(t0-Ts/2))+(n*pi*Q*Vo/2*fr/fs+Vin+n*Vo)*cos(wr*t0)-2*Vin];
end