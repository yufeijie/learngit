function x = solve_CACboost_D0(Vo_, Io_, fs_, Lr_, Cr_)
global Vo Io fs Lr Zr
Vo = Vo_;
Io = Io_;
fs = fs_;
Lr = Lr_;
Zr = sqrt(Lr_/Cr_);

opt = optimset('Display', 'off');
x = fsolve(@functionSRC, 0.1, opt);
end

function F = functionSRC(x)
global Vo Io fs Lr
D0 = x(1);
F = Lr*fs*2*(Io-ILrmin(D0))/Vo-D0;
end

function re = ILrmin(D0)
global Vo Zr
re = -sqrt(Vo^2-Vcc(D0)^2)/Zr;
end

function re = Vcc(D0)
global Vo
re = D0/(1-D0)*Vo;
end