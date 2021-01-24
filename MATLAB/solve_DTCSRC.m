function x = solve_DTCSRC(Q_, M_, Tdead_)
global Q M Tdead
Q = Q_;
M = M_;
Tdead = Tdead_;
opt = optimset('Display', 'off');
x = fsolve(@functionSRC, [0.1 1], opt);
end

function F = functionSRC(x)
global Q M Tdead
Td = x(1); fs = x(2);
F = [M-Mgain(Td, fs, Q);
     Td+Tdead*fs+B(Td, fs, Q, M)*fs/(2*pi)-0.5];
end

function re = B(Td, fs, Q, M)
 re = pi-BP(Td, fs, Q, M);
if CCMflag(Td, fs, Q, M) == 1
    BPccm = BP(Td, fs, Q, M);
    re = re-asin(R3(Td, fs, Q, M)*sin(pi/fs-2*pi*Td/fs+BPccm)/2);
end
end

function re = R3(Td, fs, Q, M)
re = Vcrpk(Td, fs, Q, M)+M+1;
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