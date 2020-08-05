function x = solve_DTCSRC_Td(Q_, M_, fs_)
global Q M fs
Q = Q_;
M = M_;
fs = fs_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, 0.1, opt);
end

function F = functionSRC(x)
global Q M fs
Td = x(1);
F = M-Mgain(Td, fs, Q);
end

function re = Mgain(Td, fs, Q)
re = solve_DTCSRC_Mgain1(Td, fs, Q);
if CCMflag(Td, fs, Q, re) == 0
	re = solve_DTCSRC_Mgain2(Td, fs, Q);
end
end

function re = CCMflag(Td, fs, Q, M)
BPccm = BP(Td, fs, Q, M);
if sin(pi/fs-2*pi*Td/fs+BPccm) >= 0
	re = 1;
else
	re = 0;
end
end

function re = BP(Td, fs, Q, M)
re = atanx((Vcrpk(Td, fs, Q, M)+1)*sin(2*pi*Td/fs)/((Vcrpk(Td, fs, Q, M)+1)*cos(2*pi*Td/fs)-M));
end

function re = Vcrpk(Td, fs, Q, M)
re = (1-cos(2*pi*Td/fs)+pi/fs*Q*M)/(1+cos(2*pi*Td/fs));
end

function re = atanx(x)
re = atan(x);
if re < 0
    re = re+pi;
end
end