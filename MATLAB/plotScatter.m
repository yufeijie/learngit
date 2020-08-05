function plotScatter(titleName, xLabelName, yLabelName, len, x, y, xBest, yBest)
% plotScatter ��ɢ��ͼ
% titleName ������
% xLabelName yLabelName zLabelName ��������
% len ������
% x y ɢ������
% xBest yBest ��������
figure(5)
if len > 0
scatter(x(1:len), y(1:len), 10);
hold on
end
scatter(xBest, yBest, 20, 'r', 'filled');
hold on
plot(xBest, yBest, 'r');
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
end

