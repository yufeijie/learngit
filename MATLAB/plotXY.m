function plotXY(titleName, xLabelName, yLabelName, x, y)
% plotXYZ ����ά����ͼ
% titleName ������
% xLabelName X����
% yLabelName Y����
% zLabelName Z����
% x y ����
figure(1)
plot(x, y)
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
end