function plotXYZ(titleName, xLabelName, yLabelName, zLabelName, n, len, x, y, z)
% plotXYZ ����ά����ͼ
% titleName ������
% xLabelName X����
% yLabelName Y����
% zLabelName Z����
% n ����ϵ����
% len ���ݳ���
% x y z ����
figure(1)
for i = 1:n
    xx = x(i, 1:len(i));
    yy = y(i, 1:len(i));
    zz = z(i, 1:len(i));
    plot3(xx, yy, zz);
    hold on
end
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end